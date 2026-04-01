using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Represents the identity and operational context of the current request.
///
/// Runtime guarantees (enforced by pipeline behaviors):
/// <list type="bullet">
///   <item><description><see cref="UserId"/>.Value is always &gt; 0 when <see cref="IsAuthenticated"/> is true.</description></item>
///   <item><description><see cref="CustomerId"/>.Value is always &gt; 0 for authenticated operational requests.</description></item>
///   <item><description><see cref="ProfileId"/>.Value is always &gt; 0 for requests marked <c>IRequireProfile</c> — enforced by <c>RequireProfileBehavior</c>.</description></item>
///   <item><description><see cref="LanguageId"/> is resolved from the active runtime context (UserCustomerMembership row).</description></item>
///   <item><description><see cref="OriginalUserId"/> is null unless impersonation is active.</description></item>
///   <item><description><see cref="IsImpersonating"/> is true if and only if <see cref="OriginalUserId"/> has a value.</description></item>
/// </list>
///
/// Forbidden states (never valid):
/// <list type="bullet">
///   <item><description><see cref="CustomerId"/>.Value == 0 — not a valid operational context.</description></item>
///   <item><description><see cref="ProfileId"/>.Value == 0 — not a valid operational context for business operations.</description></item>
///   <item><description>Nullable <see cref="ProfileId"/> or <see cref="CustomerId"/> — both are non-nullable by design.</description></item>
/// </list>
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's identity. Value &gt; 0 when <see cref="IsAuthenticated"/> is true.</summary>
    UserId UserId { get; }

    /// <summary>The active tenant context. Value &gt; 0 for authenticated operational requests.</summary>
    CustomerId CustomerId { get; }

    /// <summary>
    /// The active profile context. Value &gt; 0 for business operations.
    /// Enforced at pipeline level by <c>RequireProfileBehavior</c> for requests marked <c>IRequireProfile</c>.
    /// </summary>
    ProfileId ProfileId { get; }

    /// <summary>Language resolved from UserCustomerMembership for the active tenant context.</summary>
    int LanguageId { get; }

    /// <summary>The authenticated user's email address.</summary>
    string Email { get; }

    /// <summary>True if and only if <see cref="OriginalUserId"/> has a value.</summary>
    bool IsImpersonating { get; }

    /// <summary>The original user's identity when impersonating. Null when not impersonating.</summary>
    UserId? OriginalUserId { get; }

    /// <summary>True when the request carries a valid, authenticated principal.</summary>
    bool IsAuthenticated { get; }
}
