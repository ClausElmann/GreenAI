using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Api.V1.Auth.Token;

public sealed class GetApiTokenHandler(IGetApiTokenRepository repository, JwtTokenService jwt)
    : IRequestHandler<GetApiTokenCommand, Result<GetApiTokenResponse>>
{
    public async Task<Result<GetApiTokenResponse>> Handle(GetApiTokenCommand command, CancellationToken ct)
    {
        // Single query validates: user exists + has API role + is member of CustomerId + has access to ProfileId
        var user = await repository.FindUserAsync(command.Email, command.CustomerId, command.ProfileId);

        // Deliberately vague — do not reveal whether email, role, customer, or profile is the mismatch
        if (user is null)
            return Result<GetApiTokenResponse>.Fail("INVALID_CREDENTIALS", "Invalid credentials or insufficient API access.");

        if (user.IsLockedOut)
            return Result<GetApiTokenResponse>.Fail("ACCOUNT_LOCKED", "Account is locked. Contact support.");

        if (!PasswordHasher.Verify(command.Password, user.PasswordHash, user.PasswordSalt))
        {
            await repository.RecordFailedLoginAsync(new UserId(user.Id));
            return Result<GetApiTokenResponse>.Fail("INVALID_CREDENTIALS", "Invalid credentials or insufficient API access.");
        }

        if (user.FailedLoginCount > 0)
            await repository.ResetFailedLoginAsync(new UserId(user.Id));

        var userId     = new UserId(user.Id);
        var customerId = new CustomerId(command.CustomerId);
        var profileId  = new ProfileId(command.ProfileId);

        var token = jwt.CreateToken(userId, customerId, profileId, user.Email, user.LanguageId);

        await repository.SaveRefreshTokenAsync(
            userId, customerId, profileId,
            token.RefreshToken, token.ExpiresAt.AddDays(30), user.LanguageId);

        return Result<GetApiTokenResponse>.Ok(
            new GetApiTokenResponse(token.AccessToken, token.ExpiresAt, token.RefreshToken));
    }
}
