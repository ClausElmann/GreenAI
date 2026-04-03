using Dapper;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Features.Auth;

/// <summary>
/// Integration tests for SelectProfileRepository — verifies the SQL files, not the handler logic.
///
/// Critical behaviour being tested:
///   - GetAvailableProfiles.sql returns accessible profiles for the current user+customer only
///   - No profile from another customer is returned (tenant isolation)
///   - ProfileId returned is always > 0 (real Profiles.Id value)
///   - SaveRefreshToken.sql persists ProfileId + LanguageId in UserRefreshTokens
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SelectProfileRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public SelectProfileRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static SelectProfileRepository CreateRepository() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // GetAvailableProfiles.sql
    // RULE: Returns accessible profiles for (UserId, CustomerId) — no cross-tenant leakage.
    // ===================================================================

    [Fact]
    public async Task GetAvailableProfilesAsync_UserWithOneProfile_ReturnsOneRecord()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "alice@example.com" });
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Primary Profile");

        var results = await CreateRepository().GetAvailableProfilesAsync(userId, customerId);

        Assert.Single(results);
        Assert.Equal(profileId, results.First().ProfileId);
        Assert.True(results.First().ProfileId > 0); // ProfileId must always be > 0
        Assert.Equal("Primary Profile", results.First().DisplayName);
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_UserWithMultipleProfiles_ReturnsAll()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "bob@example.com" });
        await _builder.InsertProfileAsync(customerId, userId, "Work");
        await _builder.InsertProfileAsync(customerId, userId, "Personal");

        var results = await CreateRepository().GetAvailableProfilesAsync(userId, customerId);

        Assert.Equal(2, results.Count);
        Assert.All(results, p => Assert.True(p.ProfileId > 0));
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_UserWithNoProfiles_ReturnsEmpty()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();

        var results = await CreateRepository().GetAvailableProfilesAsync(userId, customerId);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_DoesNotReturnProfilesFromOtherCustomer()
    {
        // TENANCY RULE: WHERE CustomerId = @CustomerId must prevent cross-customer profile leakage.
        var customerA = await _builder.InsertCustomerAsync("Customer A");
        var customerB = await _builder.InsertCustomerAsync("Customer B");
        var userId = await _builder.InsertUserAsync(new() { Email = "tenant@example.com" });
        await _builder.InsertProfileAsync(customerA, userId, "Profile for A");

        var results = await CreateRepository().GetAvailableProfilesAsync(userId, customerB);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_DoesNotReturnProfilesFromOtherUser()
    {
        // One user's profiles must not be visible to another user in the same customer.
        var customerId = await _builder.InsertCustomerAsync();
        var userA = await _builder.InsertUserAsync(new() { Email = "a@example.com" });
        var userB = await _builder.InsertUserAsync(new() { Email = "b@example.com" });
        await _builder.InsertProfileAsync(customerId, userA, "User A Profile");

        var results = await CreateRepository().GetAvailableProfilesAsync(userB, customerId);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_ProfileExistsButNoMappingForUser_ReturnsEmpty()
    {
        // A profile row exists in the customer but the requesting user has no mapping to it.
        // RULE: access is governed exclusively by ProfileUserMappings — no direct Profiles.UserId ownership.
        var customerId = await _builder.InsertCustomerAsync();
        var ownerUser = await _builder.InsertUserAsync(new() { Email = "owner@example.com" });
        var otherUser = await _builder.InsertUserAsync(new() { Email = "other@example.com" });
        await _builder.InsertProfileAsync(customerId, ownerUser, "Owner Only Profile");

        // otherUser has no ProfileUserMappings row for this profile
        var results = await CreateRepository().GetAvailableProfilesAsync(otherUser, customerId);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAvailableProfilesAsync_ProfileMappedToMultipleUsers_AllUsersCanAccess()
    {
        // Many-to-many: same profile is accessible by two different users.
        var customerId = await _builder.InsertCustomerAsync();
        var userA = await _builder.InsertUserAsync(new() { Email = "shared-a@example.com" });
        var userB = await _builder.InsertUserAsync(new() { Email = "shared-b@example.com" });

        // Insert profile for userA; then grant userB access via mapping
        var profileId = await _builder.InsertProfileAsync(customerId, userA, "Shared Profile");
        await _builder.InsertProfileUserMappingAsync(userB, profileId);

        var resultsA = await CreateRepository().GetAvailableProfilesAsync(userA, customerId);
        var resultsB = await CreateRepository().GetAvailableProfilesAsync(userB, customerId);

        Assert.Single(resultsA);
        Assert.Equal(profileId, resultsA.First().ProfileId);
        Assert.Single(resultsB);
        Assert.Equal(profileId, resultsB.First().ProfileId);
    }
    // NOTE: SaveRefreshToken SQL tests removed.
    // SaveAsync is now tested via SharedKernel/Auth/RefreshTokenWriterTests (Slice 1).
}
