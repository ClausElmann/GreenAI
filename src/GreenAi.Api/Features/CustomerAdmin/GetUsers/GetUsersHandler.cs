using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.CustomerAdmin.GetUsers;

public record GetUsersQuery : IRequest<Result<List<UserRow>>>, IRequireAuthentication, IRequireProfile;

public record UserRow(int Id, string Email, bool IsActive);
public record UserProfileRow(int ProfileId, string ProfileName);
public record UserRoleRow(string Role, string ProfileName);

public sealed class GetUsersHandler(IDbSession db, ICurrentUser user)
    : IRequestHandler<GetUsersQuery, Result<List<UserRow>>>
{
    public async Task<Result<List<UserRow>>> Handle(GetUsersQuery _, CancellationToken ct)
    {
        var sql  = SqlLoader.Load<GetUsersHandler>("GetUsersForCustomer.sql");
        var rows = await db.QueryAsync<UserRow>(sql, new { CustomerId = user.CustomerId.Value });
        return Result<List<UserRow>>.Ok(rows.ToList());
    }
}
