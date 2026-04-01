namespace GreenAi.Api.SharedKernel.Permissions;

/// <summary>
/// Well-known ProfileRole names used for operational capability checks.
/// Only add names that are actively referenced by handlers.
/// Source: FOUNDATIONAL_DOMAIN_ANALYSIS — 63 roles in source system.
/// RULE: Do not add all 63 speculatively — grow this list as feature handlers are implemented.
/// </summary>
public static class ProfileRoleNames
{
    public const string HaveNoSendRestrictions      = "HaveNoSendRestrictions";
    public const string CanSendByEboks              = "CanSendByEboks";
    public const string CanSendByVoice              = "CanSendByVoice";
    public const string UseMunicipalityPolList       = "UseMunicipalityPolList";
    public const string CanSendToCriticalAddresses  = "CanSendToCriticalAddresses";
    public const string SmsConversations            = "SmsConversations";
}
