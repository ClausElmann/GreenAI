using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Localization.BatchUpsertLabels;

public sealed class BatchUpsertLabelsHandler(
    IBatchUpsertLabelsRepository repository,
    ICurrentUser user,
    IPermissionService permissions)
    : IRequestHandler<BatchUpsertLabelsCommand, Result<BatchUpsertLabelsResponse>>
{
    public async Task<Result<BatchUpsertLabelsResponse>> Handle(
        BatchUpsertLabelsCommand command, CancellationToken ct)
    {
        // Authentication is enforced by AuthorizationBehavior (IRequireAuthentication marker).
        // The SuperAdmin check here is a feature-specific authorization decision — not cross-cutting.
        var isSuperAdmin = await permissions.IsUserSuperAdminAsync(user.UserId);
        if (!isSuperAdmin)
            return Result<BatchUpsertLabelsResponse>.Fail("FORBIDDEN", "SuperAdmin role required.");

        if (command.Labels.Count == 0)
            return Result<BatchUpsertLabelsResponse>.Ok(new BatchUpsertLabelsResponse(0));

        // Intentional N+1: admin-only, max 500 labels (enforced by validator), auditable per-row.
        // Z.Dapper.Plus BulkMerge is available if volume requirements change.
        var count = 0;
        foreach (var label in command.Labels)
        {
            await repository.UpsertAsync(label);
            count++;
        }

        return Result<BatchUpsertLabelsResponse>.Ok(new BatchUpsertLabelsResponse(count));
    }
}
