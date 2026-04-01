namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// A resolved profile accessible to the current user within the active customer.
/// Used in multi-profile selection responses (LoginResponse, SelectCustomerResponse, SelectProfileResponse).
/// ProfileId is always > 0 — this record is never constructed for an unresolved profile.
/// </summary>
public sealed record ProfileSummary(int ProfileId, string DisplayName);
