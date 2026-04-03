using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Authoritative single write path for persisting refresh tokens.
///
/// Used by all auth features that issue tokens:
///   Login, SelectCustomer, SelectProfile, RefreshToken, GetApiToken.
///
/// This eliminates the SaveRefreshToken.sql duplication that previously existed
/// across Feature/Auth/Login/, SelectCustomer/, and SelectProfile/.
/// </summary>
public interface IRefreshTokenWriter
{
    Task SaveAsync(
        UserId userId,
        CustomerId customerId,
        ProfileId profileId,
        string token,
        DateTimeOffset expiresAt,
        int languageId);
}
