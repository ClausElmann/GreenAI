using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Permissions;

/// <summary>
/// Role-check service for user-level and profile-level capability gates.
///
/// Two independent capability systems exist:
///   - <see cref="DoesUserHaveRoleAsync"/> — global admin/UI roles (UserRoleMappings, no CustomerId).
///     SuperAdmin bypasses all user-level checks via separate method.
///   - <see cref="DoesProfileHaveRoleAsync"/> — operational capability flags (ProfileRoleMappings).
///     This is the PRIMARY enforcement mechanism for all send/feature gates.
///
/// RULE: Never add parameters to the role-check methods for filtering by customer or profile context.
/// UserRoles are global by design (FOUNDATIONAL_DOMAIN_ANALYSIS — CONTRADICTION_003 acknowledged,
/// Option D migration deferred). ProfileRoles are scoped to a single Profile.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns true if the user has the specified global admin role.
    /// SuperAdmin is NOT automatically included — check it explicitly when needed.
    /// </summary>
    Task<bool> DoesUserHaveRoleAsync(UserId userId, string roleName);

    /// <summary>
    /// Returns true if the user has the SuperAdmin role.
    /// SuperAdmin bypasses most authorization checks. Use only for admin-level gates.
    /// </summary>
    Task<bool> IsUserSuperAdminAsync(UserId userId);

    /// <summary>
    /// Returns true if the profile has the specified operational capability flag.
    /// This is the PRIMARY feature gate for all messaging/operational features.
    /// </summary>
    Task<bool> DoesProfileHaveRoleAsync(ProfileId profileId, string roleName);
}
