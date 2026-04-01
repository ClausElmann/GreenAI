using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.SelectProfile;

public interface ISelectProfileRepository
{
    Task<IReadOnlyCollection<ProfileRecord>> GetAvailableProfilesAsync(UserId userId, CustomerId customerId);
    Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId);
}

/// <summary>
/// A row from the Profiles table accessible to the current user+customer.
/// ProfileId is always > 0 — this record is only created from real Profiles.Id values.
/// </summary>
public sealed record ProfileRecord(int ProfileId, string DisplayName);
