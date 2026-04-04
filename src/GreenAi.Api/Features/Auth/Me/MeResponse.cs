namespace GreenAi.Api.Features.Auth.Me;

/// <summary>
/// Identity context for the authenticated user.
/// All values are resolved from JWT claims by ICurrentUser.
/// LanguageId comes from the UserCustomerMembership row at token-issue time.
/// </summary>
public sealed record MeResponse(
    int UserId,
    int CustomerId,
    int ProfileId,
    int LanguageId,
    string Email,
    bool IsImpersonating,
    int? OriginalUserId);
