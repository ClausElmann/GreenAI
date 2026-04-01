using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.RefreshToken;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IRefreshTokenRepository _repository;
    private readonly JwtTokenService _jwt;

    public RefreshTokenHandler(IRefreshTokenRepository repository, JwtTokenService jwt)
    {
        _repository = repository;
        _jwt = jwt;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var record = await _repository.FindValidTokenAsync(request.RefreshToken);
        if (record is null)
            return Result<RefreshTokenResponse>.Fail("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired");

        // Single-use: revoke the used token immediately
        await _repository.RevokeTokenAsync(record.Id);

        // ProfileId is read from the stored token — it carries the resolved profile from the
        // original login/select-profile flow.
        //
        // Migration note (V008 DEFAULT 0): tokens stored before Step 11 carry ProfileId = 0.
        // Refreshing those tokens produces a new JWT with ProfileId = 0, which is intentional:
        //   - Auth endpoints (Login, SelectCustomer, SelectProfile, RefreshToken) remain accessible.
        //   - Protected business operations are blocked by RequireProfileBehavior (ProfileId guaranteed
        //     by RequireProfileBehavior — IRequireProfile-marked requests will return PROFILE_NOT_SELECTED).
        //   - The user is directed to call SelectProfile to obtain a real ProfileId > 0.
        // No conditional bypass exists here. ProfileId = 0 simply cannot reach protected operations.
        var profileId = new ProfileId(record.ProfileId);

        var token = _jwt.CreateToken(
            new UserId(record.UserId),
            new CustomerId(record.CustomerId),
            profileId,
            record.Email,
            record.LanguageId);

        // Issue new refresh token (rotation)
        await _repository.SaveNewTokenAsync(
            new UserId(record.UserId),
            new CustomerId(record.CustomerId),
            profileId,
            token.RefreshToken,
            DateTimeOffset.UtcNow.AddDays(30),
            record.LanguageId);

        return Result<RefreshTokenResponse>.Ok(new RefreshTokenResponse(
            token.AccessToken,
            token.ExpiresAt,
            token.RefreshToken));
    }
}
