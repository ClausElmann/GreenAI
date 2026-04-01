using GreenAi.Api.SharedKernel.Auth;

namespace GreenAi.Api.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    bool NeedsCustomerSelection = false,
    bool NeedsProfileSelection = false,
    IReadOnlyCollection<ProfileSummary>? AvailableProfiles = null)
{
    public static LoginResponse WithToken(string accessToken, DateTimeOffset expiresAt, string refreshToken) =>
        new(accessToken, expiresAt, refreshToken);

    public static LoginResponse RequiresCustomerSelection() =>
        new(string.Empty, DateTimeOffset.MinValue, string.Empty, NeedsCustomerSelection: true);

    public static LoginResponse RequiresProfileSelection(IReadOnlyCollection<ProfileSummary> profiles) =>
        new(string.Empty, DateTimeOffset.MinValue, string.Empty, NeedsProfileSelection: true, AvailableProfiles: profiles);
}

