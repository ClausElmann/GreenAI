namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Claim type constants. Use these everywhere — never magic strings.
/// </summary>
public static class GreenAiClaims
{
    public const string CustomerId = "greenai:customer_id";
    public const string ProfileId = "greenai:profile_id";
    public const string LanguageId = "greenai:language_id";
    public const string ImpersonatedUserId = "greenai:impersonated_user_id";
}
