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
///   - FindMembership.sql filters on IsActive = 1 and returns LanguageId
///   - GetProfiles.sql returns profiles for the current user+customer only (tenant isolation)
///   - SaveRefreshToken.sql persists LanguageId AND ProfileId in UserRefreshTokens
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
    // RULE: Returns the membership row for an active UserCustomerMembership.
    //       Returns NULL if inactive or not found.
    //       No profile data — profile resolution is a separate step (GetProfilesAsync).
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
    // GetProfiles.sql
    // RULE: Returns accessible profiles for the current user+customer.
    //       No cross-tenant leakage (WHERE CustomerId = @CustomerId).
    // ===================================================================

    [Fact]
    public async Task GetProfilesAsync_UserWithOneProfile_ReturnsOneRecord()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "profile-user@example.com" });
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Primary");

        var results = await CreateRepository().GetProfilesAsync(userId, customerId);

        Assert.Single(results);
        Assert.Equal(profileId, results.First().ProfileId);
        Assert.True(results.First().ProfileId > 0);
        Assert.Equal("Primary", results.First().DisplayName);
    }

    [Fact]
    public async Task GetProfilesAsync_UserWithNoProfiles_ReturnsEmpty()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();

        var results = await CreateRepository().GetProfilesAsync(userId, customerId);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetProfilesAsync_DoesNotReturnProfilesFromOtherCustomer()
    {
        // TENANCY RULE: WHERE CustomerId = @CustomerId must prevent cross-customer profile leakage.
        var customerA = await _builder.InsertCustomerAsync("Customer A");
        var customerB = await _builder.InsertCustomerAsync("Customer B");
        var userId = await _builder.InsertUserAsync(new() { Email = "tenant@example.com" });
        await _builder.InsertProfileAsync(customerA, userId, "Profile for A");

        var results = await CreateRepository().GetProfilesAsync(userId, customerB);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetProfilesAsync_ProfileExistsButNoMappingForUser_ReturnsEmpty()
    {
        // A profile row exists for the customer but the requesting user has no ProfileUserMappings entry.
        var customerId = await _builder.InsertCustomerAsync();
        var ownerUser = await _builder.InsertUserAsync(new() { Email = "owner-sc@example.com" });
        var otherUser = await _builder.InsertUserAsync(new() { Email = "other-sc@example.com" });
        await _builder.InsertProfileAsync(customerId, ownerUser, "Owner Only Profile");

        var results = await CreateRepository().GetProfilesAsync(otherUser, customerId);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetProfilesAsync_ProfileMappedToMultipleUsers_AllUsersCanAccess()
    {
        // Many-to-many: the same profile is accessible by two users.
        var customerId = await _builder.InsertCustomerAsync();
        var userA = await _builder.InsertUserAsync(new() { Email = "sc-shared-a@example.com" });
        var userB = await _builder.InsertUserAsync(new() { Email = "sc-shared-b@example.com" });

        var profileId = await _builder.InsertProfileAsync(customerId, userA, "Shared Profile");
        await _builder.InsertProfileUserMappingAsync(userB, profileId);

        var resultsA = await CreateRepository().GetProfilesAsync(userA, customerId);
        var resultsB = await CreateRepository().GetProfilesAsync(userB, customerId);

        Assert.Single(resultsA);
        Assert.Equal(profileId, resultsA.First().ProfileId);
        Assert.Single(resultsB);
        Assert.Equal(profileId, resultsB.First().ProfileId);
    }

    // ===================================================================
    // SaveRefreshToken.sql
    // RULE: Inserts a UserRefreshToken row with LanguageId AND ProfileId persisted.
    //       ProfileId must be a real Profiles.Id (> 0).
    // ===================================================================

    [Fact]
    public async Task SaveRefreshTokenAsync_ValidArgs_PersistsProfileId()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var rawProfileId = await _builder.InsertProfileAsync(customerId, userId);
        var profileId = new ProfileId(rawProfileId);

        await CreateRepository().SaveRefreshTokenAsync(userId, customerId, profileId, "sc-token", DateTimeOffset.UtcNow.AddDays(30), languageId: 3);

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var storedProfileId = await conn.ExecuteScalarAsync<int>(
            "SELECT ProfileId FROM UserRefreshTokens WHERE Token = @Token",
            new { Token = "sc-token" });
        Assert.Equal(rawProfileId, storedProfileId);
        Assert.True(storedProfileId > 0);
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ValidArgs_PersistsLanguageId()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var profileId = new ProfileId(await _builder.InsertProfileAsync(customerId, userId));

        await CreateRepository().SaveRefreshTokenAsync(userId, customerId, profileId, "sc-lang-token", DateTimeOffset.UtcNow.AddDays(30), languageId: 3);

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var storedLanguageId = await conn.ExecuteScalarAsync<int>(
            "SELECT LanguageId FROM UserRefreshTokens WHERE Token = @Token",
            new { Token = "sc-lang-token" });
        Assert.Equal(3, storedLanguageId);
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ValidArgs_TokenIsRetrievable()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var profileId = new ProfileId(await _builder.InsertProfileAsync(customerId, userId));

        await CreateRepository().SaveRefreshTokenAsync(userId, customerId, profileId, "sc-findable-token", DateTimeOffset.UtcNow.AddDays(30), languageId: 1);

        var count = await _builder.CountRefreshTokensByUserIdAsync(userId);
        Assert.Equal(1, count);
    }
}
