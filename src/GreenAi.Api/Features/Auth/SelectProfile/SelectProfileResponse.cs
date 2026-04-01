using GreenAi.Api.SharedKernel.Auth;

namespace GreenAi.Api.Features.Auth.SelectProfile;

public sealed record SelectProfileResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    bool NeedsProfileSelection = false,
    IReadOnlyCollection<ProfileSummary>? AvailableProfiles = null)
{
    public static SelectProfileResponse WithToken(string accessToken, DateTimeOffset expiresAt, string refreshToken) =>
        new(accessToken, expiresAt, refreshToken);

    public static SelectProfileResponse RequiresProfileSelection(IReadOnlyCollection<ProfileSummary> profiles) =>
        new(string.Empty, DateTimeOffset.MinValue, string.Empty, NeedsProfileSelection: true, AvailableProfiles: profiles);
}
