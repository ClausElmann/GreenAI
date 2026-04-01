using GreenAi.Tests;
using Microsoft.Data.SqlClient;
using Dapper;

namespace GreenAi.Tests.Database;

[Collection(DatabaseCollection.Name)]
public sealed class SchemaIntegrationTests
{
    private readonly DatabaseFixture _db;

    public SchemaIntegrationTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task Database_HasAllRequiredTables()
    {
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.OpenAsync(TestContext.Current.CancellationToken);

        var tables = (await conn.QueryAsync<string>(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"))
            .ToHashSet();

        Assert.Contains("Customers", tables);
        Assert.Contains("Users", tables);
        Assert.Contains("Profiles", tables);
        Assert.Contains("UserRefreshTokens", tables);
        Assert.Contains("Logs", tables);
    }

    [Fact]
    public async Task Database_CanInsertAndReadCustomer()
    {
        await _db.ResetAsync();

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.OpenAsync(TestContext.Current.CancellationToken);

        var id = await conn.QuerySingleAsync<int>(
            "INSERT INTO Customers (Name) OUTPUT INSERTED.Id VALUES (@Name)",
            new { Name = "Test Kunde" });

        var name = await conn.QuerySingleAsync<string>(
            "SELECT Name FROM Customers WHERE Id = @Id",
            new { Id = id });

        Assert.Equal("Test Kunde", name);
    }
}
