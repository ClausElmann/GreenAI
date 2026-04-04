using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.AssignProfile;

public sealed class AssignProfileHandler : IRequestHandler<AssignProfileCommand, Result<AssignProfileResponse>>
{
    private readonly IAssignProfileRepository _repository;
    private readonly ICurrentUser             _currentUser;
    private readonly IPermissionService       _permissions;

    public AssignProfileHandler(
        IAssignProfileRepository repository,
        ICurrentUser             currentUser,
        IPermissionService       permissions)
    {
        _repository  = repository;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<AssignProfileResponse>> Handle(AssignProfileCommand command, CancellationToken ct)
    {
        var canManage    = await _permissions.DoesUserHaveRoleAsync(_currentUser.UserId, UserRoleNames.ManageProfiles);
        var isSuperAdmin = await _permissions.IsUserSuperAdminAsync(_currentUser.UserId);

        if (!canManage && !isSuperAdmin)
            return Result<AssignProfileResponse>.Fail("FORBIDDEN", "You do not have permission to assign profiles.");

        // Tenant isolation: profile must belong to caller's customer
        if (!await _repository.ProfileBelongsToCustomerAsync(command.ProfileId, _currentUser.CustomerId))
            return Result<AssignProfileResponse>.Fail("NOT_FOUND", "Profile not found.");

        // Target user must be a member of caller's customer
        if (!await _repository.UserBelongsToCustomerAsync(new UserId(command.TargetUserId), _currentUser.CustomerId))
            return Result<AssignProfileResponse>.Fail("NOT_FOUND", "User not found in this customer.");

        await _repository.AssignProfileAsync(new UserId(command.TargetUserId), command.ProfileId);

        return Result<AssignProfileResponse>.Ok(
            new AssignProfileResponse($"Profile {command.ProfileId} assigned to user {command.TargetUserId}."));
    }
}
