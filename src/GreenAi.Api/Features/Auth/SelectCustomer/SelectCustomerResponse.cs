using GreenAi.Api.SharedKernel.Auth;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public sealed record SelectCustomerResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    bool NeedsProfileSelection = false,
    IReadOnlyCollection<ProfileSummary>? AvailableProfiles = null)
{
    public static SelectCustomerResponse WithToken(string accessToken, DateTimeOffset expiresAt, string refreshToken) =>
        new(accessToken, expiresAt, refreshToken);

    public static SelectCustomerResponse RequiresProfileSelection(IReadOnlyCollection<ProfileSummary> profiles) =>
        new(string.Empty, DateTimeOffset.MinValue, string.Empty, NeedsProfileSelection: true, AvailableProfiles: profiles);
}
