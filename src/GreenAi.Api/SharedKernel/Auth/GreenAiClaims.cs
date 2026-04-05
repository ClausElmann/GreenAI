namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Claim type constants. Use these everywhere — never magic strings or ClaimTypes.*.
///
/// Standard JWT short names (IANA registered):
///   Sub   = "sub"   — subject (user ID)
///   Name  = "name"  — display name (email until profile names are implemented)
///   Email = "email" — email address
///
/// GreenAI custom claims use short URI-style strings (greenai:*).
///
/// MapInboundClaims must be false in both JwtBearerOptions and JwtTokenService
/// so these short names are preserved when reading tokens back.
/// </summary>
public static class GreenAiClaims
{
    // ── Standard JWT claim names (IANA) ───────────────────────────────────
    public const string Sub   = "sub";   // user ID (replaces ClaimTypes.NameIdentifier)
    public const string Name  = "name";  // display name / Identity.Name (replaces ClaimTypes.Name)
    public const string Email = "email"; // email address (replaces ClaimTypes.Email)

    // ── GreenAI custom claims ─────────────────────────────────────────────
    public const string CustomerId        = "greenai:customer_id";
    public const string ProfileId         = "greenai:profile_id";
    public const string LanguageId        = "greenai:language_id";
    public const string ImpersonatedUserId = "greenai:impersonated_user_id";
}
