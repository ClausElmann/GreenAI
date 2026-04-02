using System.Security.Claims;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// ICurrentUser implementation that reads from the authenticated ClaimsPrincipal.
/// Register as Scoped. Never read HttpContext.User directly outside this class.
///
/// Properties other than IsAuthenticated throw InvalidOperationException when accessed
/// without an authenticated principal — use IsAuthenticated to guard access.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    /// <summary>Returns an authenticated principal, or throws if no authenticated context is available.</summary>
    private ClaimsPrincipal AuthenticatedPrincipal =>
        Principal ?? throw new InvalidOperationException(
            "HttpContext is not available. ICurrentUser properties cannot be accessed outside an authenticated HTTP request.");

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public UserId UserId =>
        new(int.Parse(
            AuthenticatedPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("UserId claim is missing from the authenticated principal.")));

    public CustomerId CustomerId =>
        new(int.Parse(
            AuthenticatedPrincipal.FindFirstValue(GreenAiClaims.CustomerId)
            ?? throw new InvalidOperationException("CustomerId claim is missing from the authenticated principal.")));

    public ProfileId ProfileId =>
        new(int.Parse(
            AuthenticatedPrincipal.FindFirstValue(GreenAiClaims.ProfileId)
            ?? throw new InvalidOperationException("ProfileId claim is missing from the authenticated principal.")));

    public int LanguageId =>
        int.Parse(
            AuthenticatedPrincipal.FindFirstValue(GreenAiClaims.LanguageId)
            ?? throw new InvalidOperationException("LanguageId claim is missing from the authenticated principal."));

    public string Email =>
        AuthenticatedPrincipal.FindFirstValue(ClaimTypes.Email)
        ?? throw new InvalidOperationException("Email claim is missing from the authenticated principal.");

    public bool IsImpersonating =>
        Principal?.FindFirstValue(GreenAiClaims.ImpersonatedUserId) is not null;

    public UserId? OriginalUserId
    {
        get
        {
            var value = Principal?.FindFirstValue(GreenAiClaims.ImpersonatedUserId);
            return value is not null ? new UserId(int.Parse(value)) : null;
        }
    }
}
