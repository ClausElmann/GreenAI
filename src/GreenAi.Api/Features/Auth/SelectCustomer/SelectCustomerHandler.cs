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
    private readonly IRefreshTokenWriter _tokenWriter;

    public SelectCustomerHandler(
        ISelectCustomerRepository repository,
        JwtTokenService jwt,
        ICurrentUser currentUser,
        IRefreshTokenWriter tokenWriter)
    {
        _repository  = repository;
        _jwt         = jwt;
        _currentUser = currentUser;
        _tokenWriter = tokenWriter;
    }

    public async Task<Result<SelectCustomerResponse>> Handle(SelectCustomerCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var customerId = new CustomerId(request.CustomerId);

        var membership = await _repository.FindMembershipAsync(userId, customerId);

        if (membership is null)
            return Result<SelectCustomerResponse>.Fail("MEMBERSHIP_NOT_FOUND", "No active membership found for the selected customer.");

        // Profile resolution — ProfileId(0) placeholder is forbidden from Step 11 onward.
        var profiles = await _repository.GetProfilesAsync(userId, customerId);
        var resolution = ProfileResolutionResult.Resolve(profiles);

        if (resolution.IsNotFound)
            return Result<SelectCustomerResponse>.Fail("PROFILE_NOT_FOUND", "No accessible profiles found for this customer.");

        if (resolution.NeedsSelection)
            return Result<SelectCustomerResponse>.Ok(SelectCustomerResponse.RequiresProfileSelection(resolution.Summaries));

        // Single profile — auto-resolved. ProfileId > 0 is guaranteed.
        var profileId = resolution.ResolvedId;

        var token = _jwt.CreateToken(
            userId,
            customerId,
            profileId,
            _currentUser.Email,
            membership.LanguageId);

        await _tokenWriter.SaveAsync(
            userId, customerId, profileId, token.RefreshToken, token.ExpiresAt.AddDays(30), membership.LanguageId);

        return Result<SelectCustomerResponse>.Ok(
            SelectCustomerResponse.WithToken(token.AccessToken, token.ExpiresAt, token.RefreshToken));
    }
}
