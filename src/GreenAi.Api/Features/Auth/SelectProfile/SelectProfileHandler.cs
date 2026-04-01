using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.SelectProfile;

public sealed class SelectProfileHandler : IRequestHandler<SelectProfileCommand, Result<SelectProfileResponse>>
{
    private readonly ISelectProfileRepository _repository;
    private readonly JwtTokenService _jwt;
    private readonly ICurrentUser _currentUser;

    public SelectProfileHandler(ISelectProfileRepository repository, JwtTokenService jwt, ICurrentUser currentUser)
    {
        _repository = repository;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<Result<SelectProfileResponse>> Handle(SelectProfileCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var customerId = _currentUser.CustomerId;

        var profiles = await _repository.GetAvailableProfilesAsync(userId, customerId);

        ProfileRecord? selected;

        if (request.ProfileId is > 0)
        {
            // Explicit selection — validate that the requested profile belongs to this user+customer.
            selected = profiles.FirstOrDefault(p => p.ProfileId == request.ProfileId.Value);
            if (selected is null)
                return Result<SelectProfileResponse>.Fail("PROFILE_ACCESS_DENIED", "The selected profile is not accessible.");
        }
        else if (profiles.Count == 0)
        {
            return Result<SelectProfileResponse>.Fail("PROFILE_NOT_FOUND", "No accessible profiles found for this customer.");
        }
        else if (profiles.Count == 1)
        {
            // Auto-resolve: exactly one profile — no selection required.
            selected = profiles.First();
        }
        else
        {
            // Multiple profiles, no selection provided — signal client to choose.
            // ProfileId(0) is NOT issued. No JWT is returned until a profile is explicitly selected.
            var summaries = profiles.Select(p => new ProfileSummary(p.ProfileId, p.DisplayName)).ToList();
            return Result<SelectProfileResponse>.Ok(SelectProfileResponse.RequiresProfileSelection(summaries));
        }

        // ProfileId > 0 is guaranteed here: selected came from a real Profiles.Id row.
        var profileId = new ProfileId(selected.ProfileId);
        var languageId = _currentUser.LanguageId;

        var token = _jwt.CreateToken(userId, customerId, profileId, _currentUser.Email, languageId);

        await _repository.SaveRefreshTokenAsync(
            userId, customerId, profileId, token.RefreshToken, token.ExpiresAt.AddDays(30), languageId);

        return Result<SelectProfileResponse>.Ok(
            SelectProfileResponse.WithToken(token.AccessToken, token.ExpiresAt, token.RefreshToken));
    }
}
