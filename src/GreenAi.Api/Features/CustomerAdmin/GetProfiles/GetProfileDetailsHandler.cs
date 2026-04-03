using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.CustomerAdmin.GetProfiles;

public record GetProfileDetailsQuery(int ProfileId) : IRequest<Result<ProfileRow>>, IRequireAuthentication;

public sealed class GetProfileDetailsHandler(IDbSession db, ICurrentUser user)
    : IRequestHandler<GetProfileDetailsQuery, Result<ProfileRow>>
{
    public async Task<Result<ProfileRow>> Handle(GetProfileDetailsQuery request, CancellationToken ct)
    {
        var sql = SqlLoader.Load<GetProfilesHandler>("GetProfileById.sql");
        var row = await db.QuerySingleOrDefaultAsync<ProfileRow>(sql,
            new { ProfileId = request.ProfileId, CustomerId = user.CustomerId.Value });

        return row is null
            ? Result<ProfileRow>.Fail("PROFILE_NOT_FOUND", $"Profile {request.ProfileId} not found.")
            : Result<ProfileRow>.Ok(row);
    }
}
