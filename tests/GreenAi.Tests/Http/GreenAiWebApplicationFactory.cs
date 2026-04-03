using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GreenAi.Tests.Http;

/// <summary>
/// Creates an in-memory test host for the GreenAi API.
///
/// Configuration:
/// - Uses ASPNETCORE_ENVIRONMENT=Development so appsettings.Development.json
///   is loaded (correct connection string + JWT settings).
/// - DatabaseMigrator.Run() is called during startup (idempotent — safe).
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
    }
}
