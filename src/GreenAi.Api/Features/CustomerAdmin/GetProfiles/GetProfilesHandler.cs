using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.CustomerAdmin.GetProfiles;

public record GetProfilesQuery : IRequest<Result<List<ProfileRow>>>, IRequireAuthentication;

public record ProfileRow(int Id, string Name, bool IsActive);

public sealed class GetProfilesHandler(IDbSession db, ICurrentUser user)
    : IRequestHandler<GetProfilesQuery, Result<List<ProfileRow>>>
{
    public async Task<Result<List<ProfileRow>>> Handle(GetProfilesQuery _, CancellationToken ct)
    {
        var sql  = SqlLoader.Load<GetProfilesHandler>("GetProfilesForCustomer.sql");
        var rows = await db.QueryAsync<ProfileRow>(sql, new { CustomerId = user.CustomerId.Value });
        return Result<List<ProfileRow>>.Ok(rows.ToList());
    }
}
