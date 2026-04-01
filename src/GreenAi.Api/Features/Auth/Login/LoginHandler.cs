using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Login;

public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly ILoginRepository _repository;
    private readonly JwtTokenService _jwt;

    public LoginHandler(ILoginRepository repository, JwtTokenService jwt)
    {
        _repository = repository;
        _jwt = jwt;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _repository.FindByEmailAsync(request.Email);
        if (user is null)
            return Result<LoginResponse>.Fail("INVALID_CREDENTIALS", "Email or password is incorrect");

        if (user.IsLockedOut)
            return Result<LoginResponse>.Fail("ACCOUNT_LOCKED", "Account is locked. Contact support.");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            await _repository.RecordFailedLoginAsync(new UserId(user.Id));
            return Result<LoginResponse>.Fail("INVALID_CREDENTIALS", "Email or password is incorrect");
        }

        // Reset failed login count on success
        if (user.FailedLoginCount > 0)
            await _repository.ResetFailedLoginAsync(new UserId(user.Id));

        // Post-auth tenant resolution via membership model
        var memberships = (await _repository.GetMembershipsAsync(new UserId(user.Id))).ToList();

        if (memberships.Count == 0)
            return Result<LoginResponse>.Fail("ACCOUNT_HAS_NO_TENANT", "Account has no active tenant membership.");

        if (memberships.Count > 1)
            return Result<LoginResponse>.Ok(LoginResponse.RequiresCustomerSelection());

        // Single membership — auto-select customer, then resolve profile.
        var membership = memberships[0];
        var customerId = new CustomerId(membership.CustomerId);

        // Profile resolution — ProfileId(0) placeholder is forbidden from this point onward.
        var profiles = await _repository.GetProfilesAsync(new UserId(user.Id), customerId);

        if (profiles.Count == 0)
            return Result<LoginResponse>.Fail("PROFILE_NOT_FOUND", "No accessible profiles found for this account.");

        if (profiles.Count > 1)
        {
            // Multiple profiles — client must select explicitly via /api/auth/select-profile.
            // No JWT is issued until a profile is resolved.
            var summaries = profiles.Select(p => new ProfileSummary(p.ProfileId, p.DisplayName)).ToList();
            return Result<LoginResponse>.Ok(LoginResponse.RequiresProfileSelection(summaries));
        }

        // Single profile — auto-resolve. ProfileId > 0 is guaranteed (real Profiles.Id row).
        var profileId = new ProfileId(profiles.First().ProfileId);

        var token = _jwt.CreateToken(
            new UserId(user.Id),
            customerId,
            profileId,
            user.Email,
            membership.LanguageId);

        await _repository.SaveRefreshTokenAsync(
            new UserId(user.Id), customerId, profileId, token.RefreshToken, token.ExpiresAt.AddDays(30), membership.LanguageId);

        return Result<LoginResponse>.Ok(LoginResponse.WithToken(token.AccessToken, token.ExpiresAt, token.RefreshToken));
    }
}
