namespace GreenAi.Api.SharedKernel.Permissions;

/// <summary>
/// Well-known UserRole names used for authorization checks.
/// Only add names that are actively referenced by handlers.
/// Source: FOUNDATIONAL_DOMAIN_ANALYSIS — 40 roles in source system.
/// RULE: Do not add all 40 speculatively — grow this list as handlers are implemented.
/// </summary>
public static class UserRoleNames
{
    public const string SuperAdmin              = "SuperAdmin";
    public const string API                     = "API";
    public const string ManageUsers             = "ManageUsers";
    public const string ManageProfiles          = "ManageProfiles";
    public const string CustomerSetup           = "CustomerSetup";
    public const string TwoFactorAuthenticate   = "TwoFactorAuthenticate";
}
