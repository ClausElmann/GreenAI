using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.CreateUser;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
{
    private readonly ICreateUserRepository _repository;
    private readonly ICurrentUser          _currentUser;
    private readonly IPermissionService    _permissions;

    public CreateUserHandler(
        ICreateUserRepository repository,
        ICurrentUser          currentUser,
        IPermissionService    permissions)
    {
        _repository  = repository;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Permission gate: caller must have ManageUsers or be SuperAdmin
        var canManage  = await _permissions.DoesUserHaveRoleAsync(_currentUser.UserId, UserRoleNames.ManageUsers);
        var isSuperAdmin = await _permissions.IsUserSuperAdminAsync(_currentUser.UserId);

        if (!canManage && !isSuperAdmin)
            return Result<CreateUserResponse>.Fail("FORBIDDEN", "You do not have permission to create users.");

        // Prevent duplicate email
        if (await _repository.EmailExistsAsync(command.Email))
            return Result<CreateUserResponse>.Fail("EMAIL_TAKEN", $"Email '{command.Email}' is already registered.");

        var (hash, salt) = PasswordHasher.Hash(command.InitialPassword);

        var newUserId = await _repository.InsertUserAsync(command.Email, hash, salt);

        // Link new user to caller's customer
        await _repository.InsertMembershipAsync(newUserId, _currentUser.CustomerId, command.LanguageId);

        return Result<CreateUserResponse>.Ok(new CreateUserResponse(newUserId, "User created successfully."));
    }
}
