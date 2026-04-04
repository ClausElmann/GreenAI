using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.AssignRole;

public sealed class AssignRoleHandler : IRequestHandler<AssignRoleCommand, Result<AssignRoleResponse>>
{
    private readonly IAssignRoleRepository _repository;
    private readonly ICurrentUser          _currentUser;
    private readonly IPermissionService    _permissions;

    public AssignRoleHandler(
        IAssignRoleRepository repository,
        ICurrentUser          currentUser,
        IPermissionService    permissions)
    {
        _repository  = repository;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<AssignRoleResponse>> Handle(AssignRoleCommand command, CancellationToken ct)
    {
        var canManage    = await _permissions.DoesUserHaveRoleAsync(_currentUser.UserId, UserRoleNames.ManageUsers);
        var isSuperAdmin = await _permissions.IsUserSuperAdminAsync(_currentUser.UserId);

        if (!canManage && !isSuperAdmin)
            return Result<AssignRoleResponse>.Fail("FORBIDDEN", "You do not have permission to assign roles.");

        if (!await _repository.RoleExistsAsync(command.RoleName))
            return Result<AssignRoleResponse>.Fail("NOT_FOUND", $"Role '{command.RoleName}' does not exist.");

        await _repository.AssignRoleAsync(new UserId(command.UserId), command.RoleName);

        return Result<AssignRoleResponse>.Ok(
            new AssignRoleResponse($"Role '{command.RoleName}' assigned to user {command.UserId}."));
    }
}
