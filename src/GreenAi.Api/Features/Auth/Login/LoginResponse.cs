using GreenAi.Api.SharedKernel.Auth;

namespace GreenAi.Api.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    bool NeedsCustomerSelection = false,
    bool NeedsProfileSelection = false,
    IReadOnlyCollection<CustomerSummary>? AvailableCustomers = null,
    IReadOnlyCollection<ProfileSummary>? AvailableProfiles = null,
    string? PreAuthToken = null)
{
    public static LoginResponse WithToken(string accessToken, DateTimeOffset expiresAt, string refreshToken) =>
        new(accessToken, expiresAt, refreshToken);

    public static LoginResponse RequiresCustomerSelection(string preAuthToken, IReadOnlyCollection<CustomerSummary> customers) =>
        new(string.Empty, DateTimeOffset.MinValue, string.Empty,
            NeedsCustomerSelection: true,
            AvailableCustomers: customers,
            PreAuthToken: preAuthToken);

    public static LoginResponse RequiresProfileSelection(string preAuthToken, IReadOnlyCollection<ProfileSummary> profiles) =>
        new(string.Empty, DateTimeOffset.MinValue, string.Empty,
            NeedsProfileSelection: true,
            AvailableProfiles: profiles,
            PreAuthToken: preAuthToken);
}

