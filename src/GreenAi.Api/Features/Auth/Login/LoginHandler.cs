using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Login;

public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly ILoginRepository _repository;
    private readonly JwtTokenService _jwt;
    private readonly IRefreshTokenWriter _tokenWriter;
    private readonly IDbSession _db;

    public LoginHandler(ILoginRepository repository, JwtTokenService jwt, IRefreshTokenWriter tokenWriter, IDbSession db)
    {
        _repository  = repository;
        _jwt         = jwt;
        _tokenWriter = tokenWriter;
        _db          = db;
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

        // Post-auth tenant resolution via membership model
        var memberships = (await _repository.GetMembershipsAsync(new UserId(user.Id))).ToList();

        if (memberships.Count == 0)
            return Result<LoginResponse>.Fail("ACCOUNT_HAS_NO_TENANT", "Account has no active tenant membership.");

        if (memberships.Count > 1)
        {
            // Multiple customers — issue pre-auth token with userId only (customerId=0).
            // The token is valid for /api/auth/select-customer which only needs userId.
            var preAuthToken = _jwt.CreatePreAuthToken(
                new UserId(user.Id), new CustomerId(0), user.Email, languageId: 1);
            var customers = memberships
                .Select(m => new CustomerSummary(m.CustomerId, m.CustomerName))
                .ToList();
            return Result<LoginResponse>.Ok(LoginResponse.RequiresCustomerSelection(preAuthToken, customers));
        }

        // Single membership — auto-select customer, then resolve profile.
        var membership = memberships[0];
        var customerId = new CustomerId(membership.CustomerId);

        // Profile resolution — ProfileId(0) placeholder is forbidden from this point onward.
        var profiles = await _repository.GetProfilesAsync(new UserId(user.Id), customerId);
        var resolution = ProfileResolutionResult.Resolve(profiles);

        if (resolution.IsNotFound)
            return Result<LoginResponse>.Fail("PROFILE_NOT_FOUND", "No accessible profiles found for this account.");

        if (resolution.NeedsSelection)
        {
            // Single customer, multiple profiles — issue pre-auth token with userId + customerId.
            // The token is valid for /api/auth/select-profile which needs userId + customerId.
            var preAuthToken = _jwt.CreatePreAuthToken(
                new UserId(user.Id), customerId, user.Email, membership.LanguageId);
            return Result<LoginResponse>.Ok(LoginResponse.RequiresProfileSelection(preAuthToken, resolution.Summaries));
        }

        // Single profile — auto-resolved. ProfileId > 0 is guaranteed.
        var profileId = resolution.ResolvedId;

        var token = _jwt.CreateToken(
            new UserId(user.Id),
            customerId,
            profileId,
            user.Email,
            membership.LanguageId);

        // Atomic: reset failure counter + persist refresh token in one transaction.
        // If either write fails, neither is committed — prevents inconsistent state.
        await _db.ExecuteInTransactionAsync(async () =>
        {
            if (user.FailedLoginCount > 0)
                await _repository.ResetFailedLoginAsync(new UserId(user.Id));
            await _tokenWriter.SaveAsync(
                new UserId(user.Id), customerId, profileId, token.RefreshToken, token.ExpiresAt.AddDays(30), membership.LanguageId);
        });

        return Result<LoginResponse>.Ok(LoginResponse.WithToken(token.AccessToken, token.ExpiresAt, token.RefreshToken));
    }
}
