using Dapper;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Permissions;

public sealed class PermissionService : IPermissionService
{
    private readonly IDbSession _db;

    public PermissionService(IDbSession db) => _db = db;

    public async Task<bool> DoesUserHaveRoleAsync(UserId userId, string roleName)
    {
        var result = await _db.QuerySingleOrDefaultAsync<bool?>(
            SqlLoader.Load<PermissionService>("DoesUserHaveRole.sql"),
            new { UserId = userId.Value, RoleName = roleName });
        return result ?? false;
    }

    public Task<bool> IsUserSuperAdminAsync(UserId userId)
        => DoesUserHaveRoleAsync(userId, UserRoleNames.SuperAdmin);

    public async Task<bool> DoesProfileHaveRoleAsync(ProfileId profileId, string roleName)
    {
        var result = await _db.QuerySingleOrDefaultAsync<bool?>(
            SqlLoader.Load<PermissionService>("DoesProfileHaveRole.sql"),
            new { ProfileId = profileId.Value, RoleName = roleName });
        return result ?? false;
    }

    public async Task<bool> CanUserAccessCustomerAsync(UserId userId, CustomerId customerId)
    {
        var result = await _db.QuerySingleOrDefaultAsync<bool?>(
            SqlLoader.Load<PermissionService>("CanUserAccessCustomer.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value });
        return result ?? false;
    }

    public async Task<bool> CanUserAccessProfileAsync(UserId userId, ProfileId profileId)
    {
        var result = await _db.QuerySingleOrDefaultAsync<bool?>(
            SqlLoader.Load<PermissionService>("CanUserAccessProfile.sql"),
            new { UserId = userId.Value, ProfileId = profileId.Value });
        return result ?? false;
    }
}
