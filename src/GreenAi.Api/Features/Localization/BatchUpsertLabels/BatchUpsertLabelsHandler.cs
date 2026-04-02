using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Localization.BatchUpsertLabels;

public sealed class BatchUpsertLabelsHandler(IDbSession db, ICurrentUser user, IPermissionService permissions)
    : IRequestHandler<BatchUpsertLabelsCommand, Result<BatchUpsertLabelsResponse>>
{
    public async Task<Result<BatchUpsertLabelsResponse>> Handle(
        BatchUpsertLabelsCommand command, CancellationToken ct)
    {
        if (!user.IsAuthenticated)
            return Result<BatchUpsertLabelsResponse>.Fail("UNAUTHORIZED", "Authentication required.");

        var isSuperAdmin = await permissions.IsUserSuperAdminAsync(user.UserId);
        if (!isSuperAdmin)
            return Result<BatchUpsertLabelsResponse>.Fail("FORBIDDEN", "SuperAdmin role required.");

        if (command.Labels.Count == 0)
            return Result<BatchUpsertLabelsResponse>.Ok(new BatchUpsertLabelsResponse(0));

        var upsertSql = SqlLoader.Load<BatchUpsertLabelsHandler>("BatchUpsertLabels.sql");
        var count = 0;

        // Intentional N+1: admin-only, max 500 labels (enforced by validator), auditable per-row.
        // Z.Dapper.Plus BulkMerge is available if volume requirements change.
        foreach (var label in command.Labels)
        {
            await db.ExecuteAsync(upsertSql, new
            {
                label.ResourceName,
                label.ResourceValue,
                label.LanguageId
            });
            count++;
        }

        return Result<BatchUpsertLabelsResponse>.Ok(new BatchUpsertLabelsResponse(count));
    }
}
