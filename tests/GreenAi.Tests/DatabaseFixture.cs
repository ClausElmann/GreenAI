using GreenAi.Api.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Respawn;

namespace GreenAi.Tests;

/// <summary>
/// Shared fixture that runs DbUp migrations on GreenAI_DEV once per test run,
/// then resets data (not schema) between individual tests via Respawn.
/// No separate test database — tests run against the shared dev database.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Integrated Security=true;TrustServerCertificate=true;";

    private Respawner _respawner = null!;

    public async ValueTask InitializeAsync()
    {
        // Ensure schema is up to date (idempotent — safe to run on existing DB)
        DatabaseMigrator.Run(ConnectionString, NullLogger.Instance);

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [new Respawn.Graph.Table("dbo", "SchemaVersions")]
        });
    }

    public async ValueTask ResetAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Collection marker — share one DatabaseFixture across all integration tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
