using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.GetProfileContext;

/// <summary>
/// Reads profile and customer display names for the authenticated user's active context.
/// Called by the UI shell (MainLayout) after authentication — no HTTP endpoint exists.
/// Caller is responsible for ensuring ICurrentUser is authenticated before sending this query.
/// </summary>
public sealed class GetProfileContextHandler(ICurrentUser currentUser, IDbSession db)
    : IRequestHandler<GetProfileContextQuery, Result<GetProfileContextResponse>>
{
    public async Task<Result<GetProfileContextResponse>> Handle(GetProfileContextQuery _, CancellationToken ct)
    {
        var row = await db.QuerySingleOrDefaultAsync<GetProfileContextResponse>(
            SqlLoader.Load<GetProfileContextHandler>("GetProfileContext.sql"),
            new { ProfileId = currentUser.ProfileId.Value, CustomerId = currentUser.CustomerId.Value });

        return row is not null
            ? Result<GetProfileContextResponse>.Ok(row)
            : Result<GetProfileContextResponse>.Fail("NOT_FOUND", "Profile context not found.");
    }
}
