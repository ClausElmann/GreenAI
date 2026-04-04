using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Tests.Features.Auth;

namespace GreenAi.Tests.SharedKernel.Permissions;

/// <summary>
/// Integration tests for PermissionService — verifies the SQL files against a real DB.
///
/// Critical behaviour:
///   - DoesUserHaveRoleAsync returns true only when UserRoleMappings row exists for (UserId, RoleName)
///   - DoesProfileHaveRoleAsync returns true only when ProfileRoleMappings row exists for (ProfileId, RoleName)
///   - IsUserSuperAdminAsync delegates to DoesUserHaveRoleAsync("SuperAdmin")
///   - No false positives: other users/profiles not affected
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class PermissionServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public PermissionServiceTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static PermissionService CreateService() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // DoesUserHaveRoleAsync
    // ===================================================================

    [Fact]
    public async Task DoesUserHaveRoleAsync_UserHasRole_ReturnsTrue()
    {
        var userId = await _builder.InsertUserAsync(new() { Email = "admin@example.com" });
        await _builder.AssignUserRoleAsync(userId, UserRoleNames.ManageUsers);

        var result = await CreateService().DoesUserHaveRoleAsync(userId, UserRoleNames.ManageUsers);

        Assert.True(result);
    }

    [Fact]
    public async Task DoesUserHaveRoleAsync_UserDoesNotHaveRole_ReturnsFalse()
    {
        var userId = await _builder.InsertUserAsync(new() { Email = "norole@example.com" });
        // No role assigned

        var result = await CreateService().DoesUserHaveRoleAsync(userId, UserRoleNames.ManageUsers);

        Assert.False(result);
    }

    [Fact]
    public async Task DoesUserHaveRoleAsync_UserHasDifferentRole_ReturnsFalse()
    {
        var userId = await _builder.InsertUserAsync(new() { Email = "wrongrole@example.com" });
        await _builder.AssignUserRoleAsync(userId, UserRoleNames.API);

        var result = await CreateService().DoesUserHaveRoleAsync(userId, UserRoleNames.ManageUsers);

        Assert.False(result);
    }

    [Fact]
    public async Task DoesUserHaveRoleAsync_OtherUserHasRole_ReturnsFalse()
    {
        // Verify role check is scoped to the specific userId — no false positives
        var userWithRole = await _builder.InsertUserAsync(new() { Email = "withrole@example.com" });
        var userWithout  = await _builder.InsertUserAsync(new() { Email = "withoutrole@example.com" });
        await _builder.AssignUserRoleAsync(userWithRole, UserRoleNames.SuperAdmin);

        var result = await CreateService().DoesUserHaveRoleAsync(userWithout, UserRoleNames.SuperAdmin);

        Assert.False(result);
    }

    // ===================================================================
    // IsUserSuperAdminAsync
    // ===================================================================

    [Fact]
    public async Task IsUserSuperAdminAsync_UserHasSuperAdmin_ReturnsTrue()
    {
        var userId = await _builder.InsertUserAsync(new() { Email = "superadmin@example.com" });
        await _builder.AssignUserRoleAsync(userId, UserRoleNames.SuperAdmin);

        var result = await CreateService().IsUserSuperAdminAsync(userId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserSuperAdminAsync_UserHasOtherRole_ReturnsFalse()
    {
        var userId = await _builder.InsertUserAsync(new() { Email = "notsuper@example.com" });
        await _builder.AssignUserRoleAsync(userId, UserRoleNames.ManageUsers);

        var result = await CreateService().IsUserSuperAdminAsync(userId);

        Assert.False(result);
    }

    // ===================================================================
    // DoesProfileHaveRoleAsync
    // ===================================================================

    [Fact]
    public async Task DoesProfileHaveRoleAsync_ProfileHasRole_ReturnsTrue()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId);
        await _builder.AssignProfileRoleAsync(profileId, ProfileRoleNames.CanSendByEboks);

        var result = await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileId), ProfileRoleNames.CanSendByEboks);

        Assert.True(result);
    }

    [Fact]
    public async Task DoesProfileHaveRoleAsync_ProfileDoesNotHaveRole_ReturnsFalse()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId);
        // No role assigned

        var result = await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileId), ProfileRoleNames.CanSendByEboks);

        Assert.False(result);
    }

    [Fact]
    public async Task DoesProfileHaveRoleAsync_ProfileHasDifferentRole_ReturnsFalse()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId);
        await _builder.AssignProfileRoleAsync(profileId, ProfileRoleNames.SmsConversations);

        var result = await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileId), ProfileRoleNames.CanSendByEboks);

        Assert.False(result);
    }

    [Fact]
    public async Task DoesProfileHaveRoleAsync_OtherProfileHasRole_ReturnsFalse()
    {
        // Verify role check is scoped to the specific profileId — no false positives
        var customerId       = await _builder.InsertCustomerAsync();
        var userId           = await _builder.InsertUserAsync();
        var profileWithRole  = await _builder.InsertProfileAsync(customerId, userId);
        var profileWithout   = await _builder.InsertProfileAsync(customerId, userId);
        await _builder.AssignProfileRoleAsync(profileWithRole, ProfileRoleNames.HaveNoSendRestrictions);

        var result = await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileWithout), ProfileRoleNames.HaveNoSendRestrictions);

        Assert.False(result);
    }

    [Fact]
    public async Task DoesProfileHaveRoleAsync_ProfileHasMultipleRoles_CorrectRoleReturnsTrue()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId);
        await _builder.AssignProfileRoleAsync(profileId, ProfileRoleNames.CanSendByEboks);
        await _builder.AssignProfileRoleAsync(profileId, ProfileRoleNames.CanSendByVoice);
        await _builder.AssignProfileRoleAsync(profileId, ProfileRoleNames.SmsConversations);

        Assert.True(await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileId), ProfileRoleNames.CanSendByEboks));
        Assert.True(await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileId), ProfileRoleNames.CanSendByVoice));
        Assert.False(await CreateService().DoesProfileHaveRoleAsync(new ProfileId(profileId), ProfileRoleNames.HaveNoSendRestrictions));
    }

    // ===================================================================
    // CanUserAccessCustomerAsync
    // ===================================================================

    [Fact]
    public async Task CanUserAccessCustomerAsync_ActiveMembership_ReturnsTrue()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        var result = await CreateService().CanUserAccessCustomerAsync(userId, customerId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanUserAccessCustomerAsync_NoMembership_ReturnsFalse()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        // No membership inserted

        var result = await CreateService().CanUserAccessCustomerAsync(userId, customerId);

        Assert.False(result);
    }

    [Fact]
    public async Task CanUserAccessCustomerAsync_OtherCustomer_ReturnsFalse()
    {
        var customerId      = await _builder.InsertCustomerAsync();
        var otherCustomerId = await _builder.InsertCustomerAsync("Other Customer");
        var userId          = await _builder.InsertUserAsync();
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        var result = await CreateService().CanUserAccessCustomerAsync(userId, otherCustomerId);

        Assert.False(result);
    }

    // ===================================================================
    // CanUserAccessProfileAsync
    // ===================================================================

    [Fact]
    public async Task CanUserAccessProfileAsync_MappingExists_ReturnsTrue()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId);
        // InsertProfileAsync already creates the ProfileUserMapping

        var result = await CreateService().CanUserAccessProfileAsync(userId, new ProfileId(profileId));

        Assert.True(result);
    }

    [Fact]
    public async Task CanUserAccessProfileAsync_NoMapping_ReturnsFalse()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var otherUser  = await _builder.InsertUserAsync(new() { Email = "other@test.local" });
        var profileId  = await _builder.InsertProfileAsync(customerId, otherUser);
        // userId has no mapping to this profile

        var result = await CreateService().CanUserAccessProfileAsync(userId, new ProfileId(profileId));

        Assert.False(result);
    }
}
