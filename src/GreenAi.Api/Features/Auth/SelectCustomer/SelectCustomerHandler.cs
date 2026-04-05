using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public sealed class SelectCustomerHandler : IRequestHandler<SelectCustomerCommand, Result<SelectCustomerResponse>>
{
    private readonly ISelectCustomerRepository _repository;
    private readonly JwtTokenService _jwt;
    private readonly ICurrentUser _currentUser;

    public SelectCustomerHandler(
        ISelectCustomerRepository repository,
        JwtTokenService jwt,
        ICurrentUser currentUser)
    {
        _repository  = repository;
        _jwt         = jwt;
        _currentUser = currentUser;
    }

    public async Task<Result<SelectCustomerResponse>> Handle(SelectCustomerCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var customerId = new CustomerId(request.CustomerId);

        var membership = await _repository.FindMembershipAsync(userId, customerId);

        if (membership is null)
            return Result<SelectCustomerResponse>.Fail("MEMBERSHIP_NOT_FOUND", "No active membership found for the selected customer.");

        // Profile resolution — ProfileId(0) placeholder is forbidden from this point onward.
        var profiles = await _repository.GetProfilesAsync(userId, customerId);
        var resolution = ProfileResolutionResult.Resolve(profiles);

        if (resolution.IsNotFound)
            return Result<SelectCustomerResponse>.Fail("PROFILE_NOT_FOUND", "No accessible profiles found for this customer.");

        if (resolution.NeedsSelection)
        {
            // Multiple profiles — issue pre-auth token with userId + customerId so
            // SelectProfile can identify the user when the profile is selected.
            var preAuthToken = _jwt.CreatePreAuthToken(
                userId, customerId, _currentUser.Email, membership.LanguageId);
            return Result<SelectCustomerResponse>.Ok(
                SelectCustomerResponse.RequiresProfileSelection(preAuthToken, resolution.Summaries));
        }

        // Single profile — auto-resolved. ProfileId > 0 is guaranteed.
        var profileId = resolution.ResolvedId;

        var token = _jwt.CreateToken(
            userId,
            customerId,
            profileId,
            _currentUser.Email,
            membership.LanguageId);

        await _repository.SaveRefreshTokenAsync(
            userId, customerId, token.RefreshToken, token.ExpiresAt.AddDays(30), membership.LanguageId);

        return Result<SelectCustomerResponse>.Ok(
            SelectCustomerResponse.WithToken(token.AccessToken, token.ExpiresAt, token.RefreshToken));
    }
}
