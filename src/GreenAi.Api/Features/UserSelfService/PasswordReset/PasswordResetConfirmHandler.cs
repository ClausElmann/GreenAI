using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public sealed class PasswordResetConfirmHandler
    : IRequestHandler<PasswordResetConfirmCommand, Result<PasswordResetConfirmResponse>>
{
    private readonly IPasswordResetConfirmRepository _repository;

    public PasswordResetConfirmHandler(IPasswordResetConfirmRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PasswordResetConfirmResponse>> Handle(
        PasswordResetConfirmCommand command, CancellationToken ct)
    {
        var tokenRecord = await _repository.FindTokenAsync(command.Token);

        if (tokenRecord is null)
            return Result<PasswordResetConfirmResponse>.Fail(
                "INVALID_TOKEN", "Token is invalid or has expired.");

        var (newHash, newSalt) = PasswordHasher.Hash(command.NewPassword);

        await _repository.ConfirmResetAsync(
            tokenRecord.Id,
            tokenRecord.UserId,
            newHash,
            newSalt);

        return Result<PasswordResetConfirmResponse>.Ok(
            new PasswordResetConfirmResponse("Password reset successfully."));
    }
}
