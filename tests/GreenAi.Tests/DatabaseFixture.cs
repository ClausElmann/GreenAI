using Microsoft.Data.SqlClient;
using Respawn;

namespace GreenAi.Tests;

/// <summary>
/// Shared fixture for HTTP integration tests.
/// Schema is managed by the DB project — this fixture only resets data between tests via Respawn.
/// No separate test database — tests run against the shared dev database.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Integrated Security=true;TrustServerCertificate=true;";

    private Respawner _respawner = null!;

    public async ValueTask InitializeAsync()
    {
        // Schema is managed by the DB project — no migration step needed here.
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [
                new Respawn.Graph.Table("dbo", "SchemaVersions"),
                new Respawn.Graph.Table("dbo", "UserRoles"),
                new Respawn.Graph.Table("dbo", "ProfileRoles"),
                new Respawn.Graph.Table("dbo", "Languages"),
                new Respawn.Graph.Table("dbo", "Countries"),
                new Respawn.Graph.Table("dbo", "Labels"),
                new Respawn.Graph.Table("dbo", "EmailTemplates"),
            ]
        });
    }

    public async ValueTask ResetAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
        // Re-seed id=1 rows after Respawn wipes everything.
        // Customer GreenAI (Id=1) and User claus.elmann@gmail.com (Id=1) are the
        // production admin account. Tests must use their own data with different IDs/emails.
        await ReseedAdminRowsAsync(conn);
    }

    private static async Task ReseedAdminRowsAsync(SqlConnection conn)
    {
        await using var cmd = conn.CreateCommand();

        // Customer: GreenAI — Id=1
        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Id] = 1)
            BEGIN
                SET IDENTITY_INSERT [dbo].[Customers] ON;
                INSERT INTO [dbo].[Customers] ([Id], [Name]) VALUES (1, 'GreenAI');
                SET IDENTITY_INSERT [dbo].[Customers] OFF;
            END
            """;
        await cmd.ExecuteNonQueryAsync();

        // User: claus.elmann@gmail.com — Id=1 / Password: Flipper12#
        cmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = 1)
            BEGIN
                SET IDENTITY_INSERT [dbo].[Users] ON;
                INSERT INTO [dbo].[Users] ([Id], [Email], [PasswordHash], [PasswordSalt], [IsActive])
                VALUES (1, 'claus.elmann@gmail.com',
                    'N9p1t00iogUQwhDCgeFGRQgv174X9Wjc+NKjIg7g7LdHVGGBtrK88r5jwRsfM7bQszVQV9+333ASHfJ8qKjAhg==',
                    'VlN9lBRMfoASx0x6+OrUpbA0TTHXi/X8cEpXU2mYauk=',
                    1);
                SET IDENTITY_INSERT [dbo].[Users] OFF;
            END
            """;
        await cmd.ExecuteNonQueryAsync();
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
