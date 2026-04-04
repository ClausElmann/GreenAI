using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.AdminLight.AssignRole;

public interface IAssignRoleRepository
{
    Task<bool> RoleExistsAsync(string roleName);
    Task AssignRoleAsync(UserId userId, string roleName);
}

public sealed class AssignRoleRepository : IAssignRoleRepository
{
    private readonly IDbSession _db;

    public AssignRoleRepository(IDbSession db) => _db = db;

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        var count = await _db.QuerySingleOrDefaultAsync<int>(
            SqlLoader.Load<AssignRoleRepository>("FindUserRole.sql"),
            new { RoleName = roleName });
        return count > 0;
    }

    public Task AssignRoleAsync(UserId userId, string roleName)
        => _db.ExecuteAsync(
            SqlLoader.Load<AssignRoleRepository>("InsertUserRoleMapping.sql"),
            new { UserId = userId.Value, RoleName = roleName });
}
