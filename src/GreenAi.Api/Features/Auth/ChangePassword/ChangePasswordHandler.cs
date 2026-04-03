using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    private readonly IChangePasswordRepository _repository;
    private readonly ICurrentUser _currentUser;

    public ChangePasswordHandler(IChangePasswordRepository repository, ICurrentUser currentUser)
    {
        _repository  = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<ChangePasswordResponse>> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await _repository.FindByUserIdAsync(_currentUser.UserId);

        if (user is null)
            return Result<ChangePasswordResponse>.Fail("UNAUTHORIZED", "User not found.");

        if (user.IsLockedOut)
            return Result<ChangePasswordResponse>.Fail("ACCOUNT_LOCKED", "Account is locked. Contact support.");

        if (!PasswordHasher.Verify(command.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return Result<ChangePasswordResponse>.Fail("INVALID_CREDENTIALS", "Current password is incorrect.");

        var (newHash, newSalt) = PasswordHasher.Hash(command.NewPassword);
        await _repository.UpdatePasswordAsync(_currentUser.UserId, newHash, newSalt);

        return Result<ChangePasswordResponse>.Ok(new ChangePasswordResponse("Password changed successfully."));
    }
}
