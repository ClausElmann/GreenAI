using GreenAi.Api.SharedKernel.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GreenAi.Tests.Http;

/// <summary>
/// Creates an in-memory test host for the GreenAi API.
///
/// Configuration:
/// - Uses ASPNETCORE_ENVIRONMENT=Development so appsettings.Development.json
///   is loaded (correct connection string + JWT settings).
/// - Testing:SkipStatusCodePages=true disables UseStatusCodePagesWithReExecute,
///   which would otherwise intercept 401/405 and re-execute to a Blazor page,
///   causing 400 responses instead of the expected status codes.
/// - Schema is managed by the DB project — no migration step at startup.
/// - DapperPlusSetup.Initialize() is called during startup (idempotent).
///
/// IMPORTANT: Tests that mutate DB state must call DatabaseFixture.ResetAsync()
/// via IAsyncLifetime.InitializeAsync — share the [Collection("Database")] fixture
/// for DB cleanup between tests.
///
/// See: docs/SSOT/testing/known-issues.md KI-001 (scoped services)
///      docs/SSOT/testing/known-issues.md KI-005 (JWT config must match)
/// </summary>
public sealed class GreenAiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development environment so appsettings.Development.json is loaded.
        // This ensures the local DB connection string and dev JWT key are used.
        builder.UseEnvironment("Development");

        // Disable UseStatusCodePagesWithReExecute in tests — it would intercept
        // 401/405 responses and re-execute to Blazor's /not-found page, which
        // cannot render properly in the in-memory test host and returns 400 instead.
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Testing:SkipStatusCodePages"] = "true" }));

        // Replace SmtpEmailService with NoOpEmailService — no real SMTP in tests.
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddScoped<IEmailService, NoOpEmailService>();
        });
    }
}
