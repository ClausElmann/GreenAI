using Dapper;
using GreenAi.Api.Features.Auth.SelectCustomer;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Features.Auth;

/// <summary>
/// Integration tests for SelectCustomerRepository — verifies the SQL files, not the handler logic.
///
/// Critical behaviour being tested:
///   - FindMembership.sql filters on IsActive = 1 and returns LanguageId + DefaultProfileId
///   - SaveRefreshToken.sql persists LanguageId in UserRefreshTokens
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SelectCustomerRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public SelectCustomerRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static SelectCustomerRepository CreateRepository() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // FindMembership.sql
    // RULE: Returns the membership row (+ profile COALESCE) for an active
    //       UserCustomerMembership. Returns NULL if inactive or not found.
    // ===================================================================

    [Fact]
    public async Task FindMembershipAsync_ActiveMembership_ReturnsRecord()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "alice@example.com" });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId, languageId: 2);

        var result = await CreateRepository().FindMembershipAsync(userId, customerId);

        Assert.NotNull(result);
        Assert.Equal(customerId.Value, result.CustomerId);
        Assert.Equal(2, result.LanguageId);
    }

    [Fact]
    public async Task FindMembershipAsync_UserWithNoProfile_ReturnsZeroDefaultProfileId()
    {
        // TESTING: COALESCE(p.Id, 0) — no profile row, must return 0
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        var result = await CreateRepository().FindMembershipAsync(userId, customerId);

        Assert.NotNull(result);
        Assert.Equal(0, result.DefaultProfileId);
    }

    [Fact]
    public async Task FindMembershipAsync_InactiveMembership_ReturnsNull()
    {
        // TESTING: WHERE IsActive = 1 — inactive row must be excluded
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync(
            "INSERT INTO UserCustomerMemberships (UserId, CustomerId, LanguageId, IsActive) VALUES (@UserId, @CustomerId, 1, 0)",
            new { UserId = userId.Value, CustomerId = customerId.Value });

        var result = await CreateRepository().FindMembershipAsync(userId, customerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMembershipAsync_UnknownUserId_ReturnsNull()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var unknownUserId = new UserId(99999);

        var result = await CreateRepository().FindMembershipAsync(unknownUserId, customerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMembershipAsync_WrongCustomerId_ReturnsNull()
    {
        // TESTING: both UserId AND CustomerId must match — not just one
        var customerId = await _builder.InsertCustomerAsync();
        var otherCustomerId = await _builder.InsertCustomerAsync("Other Customer");
        var userId = await _builder.InsertUserAsync();
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        var result = await CreateRepository().FindMembershipAsync(userId, otherCustomerId);

        Assert.Null(result);
    }

    // ===================================================================
    // SaveRefreshToken.sql
    // RULE: Inserts a UserRefreshToken row with LanguageId persisted.
    // ===================================================================

    [Fact]
    public async Task SaveRefreshTokenAsync_ValidArgs_PersistsLanguageId()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        await CreateRepository().SaveRefreshTokenAsync(userId, customerId, "sc-token", expiresAt, languageId: 3);

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var storedLanguageId = await conn.ExecuteScalarAsync<int>(
            "SELECT LanguageId FROM UserRefreshTokens WHERE Token = @Token",
            new { Token = "sc-token" });
        Assert.Equal(3, storedLanguageId);
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ValidArgs_TokenIsRetrievable()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();

        await CreateRepository().SaveRefreshTokenAsync(userId, customerId, "sc-findable-token", DateTimeOffset.UtcNow.AddDays(30), languageId: 1);

        var count = await _builder.CountRefreshTokensByUserIdAsync(userId);
        Assert.Equal(1, count);
    }
}
