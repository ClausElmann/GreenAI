using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.CustomerAdmin.GetUsers;

public record GetUserDetailsQuery(UserId UserId) : IRequest<Result<UserDetailsResult>>, IRequireAuthentication, IRequireProfile;

public record UserDetailsResult(
    int Id,
    string Email,
    bool IsActive,
    List<UserProfileRow> Profiles,
    List<UserRoleRow> Roles);

public sealed class GetUserDetailsHandler(IDbSession db, ICurrentUser user)
    : IRequestHandler<GetUserDetailsQuery, Result<UserDetailsResult>>
{
    public async Task<Result<UserDetailsResult>> Handle(GetUserDetailsQuery request, CancellationToken ct)
    {
        var userSql     = SqlLoader.Load<GetUsersHandler>("GetUsersForCustomer.sql");
        var profilesSql = SqlLoader.Load<GetUsersHandler>("GetUserProfileAssignments.sql");
        var rolesSql    = SqlLoader.Load<GetUsersHandler>("GetUserRoleAssignments.sql");

        // Re-use the list query filtered to the single user via customer scoping, then pick the one we want
        var allUsers = await db.QueryAsync<UserRow>(userSql, new { CustomerId = user.CustomerId.Value });
        var userRow  = allUsers.FirstOrDefault(u => u.Id == request.UserId.Value);

        if (userRow is null)
            return Result<UserDetailsResult>.Fail("NOT_FOUND", $"User {request.UserId.Value} not found in this customer.");

        var profiles = (await db.QueryAsync<UserProfileRow>(profilesSql,
            new { UserId = request.UserId.Value, CustomerId = user.CustomerId.Value })).ToList();

        var roles = (await db.QueryAsync<UserRoleRow>(rolesSql,
            new { UserId = request.UserId.Value })).ToList();

        return Result<UserDetailsResult>.Ok(new UserDetailsResult(
            userRow.Id, userRow.Email, userRow.IsActive, profiles, roles));
    }
}
