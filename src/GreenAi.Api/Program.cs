using GreenAi.Api.Components;
using GreenAi.Api.Features.Api.V1.Auth.Token;
using GreenAi.Api.Features.Auth.ChangePassword;
using GreenAi.Api.Features.Auth.Login;
using GreenAi.Api.Features.Auth.Logout;
using GreenAi.Api.Features.Auth.Me;
using GreenAi.Api.Features.Identity.ChangeUserEmail;
using GreenAi.Api.Features.Auth.RefreshToken;
using GreenAi.Api.Features.Auth.SelectCustomer;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.Features.Localization.BatchUpsertLabels;
using GreenAi.Api.Features.Localization.GetLabels;
using GreenAi.Api.Features.System.Health;
using GreenAi.Api.Features.System.Ping;
using GreenAi.Api.Features.UserSelfService.PasswordReset;
using GreenAi.Api.Features.UserSelfService.UpdateUser;
using GreenAi.Api.Features.AdminLight.CreateUser;
using GreenAi.Api.Features.AdminLight.AssignRole;
using GreenAi.Api.Features.AdminLight.AssignProfile;
using GreenAi.Api.Features.AdminLight.ListSettings;
using GreenAi.Api.Features.AdminLight.SaveSetting;
using GreenAi.Api.SharedKernel.Settings;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Localization;
using GreenAi.Api.SharedKernel.Logging;
using GreenAi.Api.SharedKernel.Email;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Tenant;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.HttpOverrides;
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
        var cs = ctx.HostingEnvironment.IsDevelopment()
            ? ctx.Configuration.GetConnectionString("Dev")!
            : ctx.Configuration.GetConnectionString("Live")!;
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

    // Blazor: detailed circuit errors in development
    if (builder.Environment.IsDevelopment())
        builder.Services.Configure<CircuitOptions>(o => o.DetailedErrors = true);

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.TopRight;
        config.SnackbarConfiguration.PreventDuplicates = true;
        config.SnackbarConfiguration.NewestOnTop = true;
        config.SnackbarConfiguration.VisibleStateDuration = 3000;
        config.SnackbarConfiguration.ShowTransitionDuration = 300;
        config.SnackbarConfiguration.HideTransitionDuration = 300;
    });
    builder.Services.AddHttpContextAccessor();
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
    builder.Services.AddScoped<BlazorPrincipalHolder>();
    builder.Services.AddScoped<GreenAi.Api.SharedKernel.State.AppState>();
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    builder.Services.AddScoped<ITenantContext, CurrentUserTenantContext>();
    builder.Services.AddScoped<JwtTokenService>();
    builder.Services.AddScoped<IRefreshTokenWriter, RefreshTokenWriter>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
    builder.Services.AddScoped<ILocalizationRepository, LocalizationRepository>();
    builder.Services.AddScoped<ILocalizationService, LocalizationService>();
    builder.Services.AddScoped<ILocalizationContext, LocalizationContext>();
    builder.Services.AddScoped<ILoginRepository, LoginRepository>();
    builder.Services.AddScoped<IChangePasswordRepository, ChangePasswordRepository>();
    builder.Services.AddScoped<IChangeUserEmailRepository, ChangeUserEmailRepository>();
    builder.Services.AddScoped<IGetApiTokenRepository, GetApiTokenRepository>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<ISelectCustomerRepository, SelectCustomerRepository>();
    builder.Services.AddScoped<ISelectProfileRepository, SelectProfileRepository>();
    builder.Services.AddScoped<IBatchUpsertLabelsRepository, BatchUpsertLabelsRepository>();
    builder.Services.AddScoped<IApplicationSettingService, ApplicationSettingService>();
    builder.Services.AddScoped<AuthenticationStateProvider, GreenAiAuthenticationStateProvider>();
    builder.Services.AddScoped<GreenAiAuthenticationStateProvider>();
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddSingleton<CircuitHandler, LoggingCircuitHandler>();
    builder.Services.AddScoped<ISystemLogger, DefaultSystemLogger>();
    builder.Services.AddTransient<OutgoingHttpClientLoggingHandler>();
    builder.Services.AddScoped<EmailTemplateRepository>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    builder.Services.AddScoped<IPasswordResetRequestRepository, PasswordResetRequestRepository>();
    builder.Services.AddScoped<IPasswordResetConfirmRepository, PasswordResetConfirmRepository>();
    builder.Services.AddScoped<ICreateUserRepository, CreateUserRepository>();
    builder.Services.AddScoped<IAssignRoleRepository, AssignRoleRepository>();
    builder.Services.AddScoped<IAssignProfileRepository, AssignProfileRepository>();

    // JWT authentication
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration is required");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // MapInboundClaims = false: keep short claim names ("sub", "email", etc.)
            // instead of remapping to long ClaimTypes.* URIs.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                // NameClaimType = "name" so Identity.Name resolves the GreenAiClaims.Name claim.
                NameClaimType = GreenAiClaims.Name,
            };
        });
    builder.Services.AddAuthorization();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "GreenAI API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Indsæt JWT token — eksempel: eyJhbGci..."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });
        });
    }

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
        cfg.AddOpenBehavior(typeof(RequireProfileBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Priority: 1) env var (test isolation / Docker), 2) Dev/Live based on environment
    var connectionString =
        System.Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? (builder.Environment.IsDevelopment()
            ? builder.Configuration.GetConnectionString("Dev")
            : builder.Configuration.GetConnectionString("Live"))
        ?? throw new InvalidOperationException("ConnectionStrings:Dev/Live is required");
    builder.Services.AddScoped<IDbSession>(_ => new DbSession(connectionString));

    var app = builder.Build();

    // Required for shared hosting / reverse proxy (IIS, nginx) — fixes scheme, IP
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} \u2192 {StatusCode} ({Elapsed:0.0}ms)";
        options.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestHost", ctx.Request.Host.Value ?? string.Empty);
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString() ?? string.Empty);
        };
    });

    // Initialize Dapper.Plus license
    DapperPlusSetup.Initialize();

    // Seed default ApplicationSettings rows (idempotent)
    using (var scope = app.Services.CreateScope())
    {
        var settingService = scope.ServiceProvider.GetRequiredService<IApplicationSettingService>();
        await settingService.CreateDefaultsAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "GreenAI API v1"));
    }
    else
    {
        app.UseHsts();
    }

    // API exception handler — active in all environments.
    // Returns JSON ProblemDetails with ErrorId for /api/** routes.
    // ErrorId correlates the HTTP response to the [Logs] table entry written by LoggingBehavior.
    // Blazor routes use the standard circuit error boundary (ReconnectModal / Error.razor).
    app.UseExceptionHandler(exApp => exApp.Run(async context =>
    {
        var feature  = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var errorId  = Guid.NewGuid();
        var logger   = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(feature?.Error, "[UnhandledException] ErrorId: {ErrorId} Path: {Path}",
            errorId, context.Request.Path);

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode  = 500;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type    = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                title   = "An unexpected error occurred.",
                status  = 500,
                errorId = errorId.ToString()
            });
        }
        else if (!app.Environment.IsDevelopment())
        {
            context.Response.Redirect("/Error");
        }
        // Development non-API: let ASP.NET developer exception page handle it
    }));

    // Skip in WebApplicationFactory test host to prevent Blazor rendering interference.
    // Production: always active. Tests: disabled via Testing:SkipStatusCodePages=true.
    if (!app.Configuration.GetValue<bool>("Testing:SkipStatusCodePages"))
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<CurrentUserMiddleware>();
    app.UseMiddleware<RequestResponseLoggingMiddleware>();
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
    LogoutEndpoint.Map(app);
    MeEndpoint.Map(app);
    ChangePasswordEndpoint.Map(app);
    ChangeUserEmailEndpoint.Map(app);
    RefreshTokenEndpoint.Map(app);
    SelectCustomerEndpoint.Map(app);
    SelectProfileEndpoint.Map(app);
    PingEndpoint.Map(app);
    HealthEndpoint.Map(app);
    BatchUpsertLabelsEndpoint.Map(app);
    GetLabelsEndpoint.Map(app);
    GetApiTokenEndpoint.Map(app);
    UpdateUserEndpoint.Map(app);
    PasswordResetRequestEndpoint.Map(app);
    PasswordResetConfirmEndpoint.Map(app);
    CreateUserEndpoint.Map(app);
    AssignRoleEndpoint.Map(app);
    AssignProfileEndpoint.Map(app);
    ListSettingsEndpoint.Map(app);
    SaveSettingEndpoint.Map(app);

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

// Expose Program to the test assembly so WebApplicationFactory<Program> can reference it
public partial class Program { }
