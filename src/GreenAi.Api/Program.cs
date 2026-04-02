using GreenAi.Api.Components;
using GreenAi.Api.Database;
using GreenAi.Api.Features.Api.V1.Auth.Token;
using GreenAi.Api.Features.Auth.Login;
using GreenAi.Api.Features.Auth.RefreshToken;
using GreenAi.Api.Features.Auth.SelectCustomer;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.Features.Localization.BatchUpsertLabels;
using GreenAi.Api.Features.System.Ping;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Localization;
using GreenAi.Api.SharedKernel.Logging;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Tenant;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Text;

// Bootstrap logger — fanger fejl inden host er klar
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — konfigureres via appsettings
    builder.Host.UseSerilog((ctx, _, config) =>
    {
        var cs = ctx.Configuration.GetConnectionString("Default")!;
        config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "GreenAi")
            .WriteTo.Console()
            .WriteTo.MSSqlServer(
                connectionString: cs,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName = "Logs",
                    AutoCreateSqlTable = false
                },
                columnOptions: SerilogColumnOptions.Build());
    });

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddMudServices();
    builder.Services.AddHttpContextAccessor();
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    builder.Services.AddScoped<ITenantContext, CurrentUserTenantContext>();
    builder.Services.AddScoped<JwtTokenService>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
    builder.Services.AddScoped<ILocalizationRepository, LocalizationRepository>();
    builder.Services.AddScoped<ILocalizationService, LocalizationService>();
    builder.Services.AddScoped<ILocalizationContext, LocalizationContext>();
    builder.Services.AddScoped<ILoginRepository, LoginRepository>();
    builder.Services.AddScoped<IGetApiTokenRepository, GetApiTokenRepository>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<ISelectCustomerRepository, SelectCustomerRepository>();
    builder.Services.AddScoped<ISelectProfileRepository, SelectProfileRepository>();
    builder.Services.AddScoped<AuthenticationStateProvider, GreenAiAuthenticationStateProvider>();
    builder.Services.AddScoped<GreenAiAuthenticationStateProvider>();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddSingleton<CircuitHandler, LoggingCircuitHandler>();

    // JWT authentication
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration is required");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
        cfg.AddOpenBehavior(typeof(RequireProfileBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is required");
    builder.Services.AddScoped<IDbSession>(_ => new DbSession(connectionString));

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} \u2192 {StatusCode} ({Elapsed:0.0}ms)";
        options.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestHost", ctx.Request.Host.Value ?? string.Empty);
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString() ?? string.Empty);
        };
    });

    // Run database migrations on startup
    DatabaseMigrator.Run(connectionString, app.Logger);

    // Initialize Dapper.Plus license
    DapperPlusSetup.Initialize();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    // INFRASTRUCTURE: client-side JavaScript error ingestion
    app.MapPost("/api/client-log", (ClientLogRequest req, ILogger<Program> logger) =>
    {
        logger.LogError("[ClientSide] {Message} — Source: {Source} | Stack: {Stack}",
            req.Message, req.Source, req.Stack);
        return Results.Ok();
    });

    // Feature endpoints
    LoginEndpoint.Map(app);
    RefreshTokenEndpoint.Map(app);
    SelectCustomerEndpoint.Map(app);
    SelectProfileEndpoint.Map(app);
    PingEndpoint.Map(app);
    BatchUpsertLabelsEndpoint.Map(app);
    GetApiTokenEndpoint.Map(app);

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

internal sealed record ClientLogRequest(string Message, string? Source, string? Stack);
