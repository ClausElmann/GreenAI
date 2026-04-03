using System.Security.Claims;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// ICurrentUser implementation that reads from the authenticated ClaimsPrincipal.
/// Register as Scoped. Never read HttpContext.User directly outside this class.
///
/// In HTTP API requests (integration tests, API endpoints): reads from IHttpContextAccessor.HttpContext.User.
/// In Blazor Server circuits (WebSocket): HttpContext is null — falls back to BlazorPrincipalHolder.
/// Blazor pages must call BlazorPrincipalHolder.Set(authState.User) before any Mediator.Send call.
///
/// Properties other than IsAuthenticated throw InvalidOperationException when accessed
/// without an authenticated principal — use IsAuthenticated to guard access.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    private readonly BlazorPrincipalHolder _blazorHolder;

    public HttpContextCurrentUser(IHttpContextAccessor accessor, BlazorPrincipalHolder blazorHolder)
    {
        _accessor     = accessor;
        _blazorHolder = blazorHolder;
    }

    // HTTP API requests → HttpContext.User (always available, has Bearer token claims)
    // Blazor Server circuits → BlazorPrincipalHolder (set by the component before Mediator.Send)
    private ClaimsPrincipal? Principal =>
        _accessor.HttpContext?.User ?? _blazorHolder.Current;

    /// <summary>Returns an authenticated principal, or throws if no authenticated context is available.</summary>
    private ClaimsPrincipal AuthenticatedPrincipal =>
        Principal ?? throw new InvalidOperationException(
            "No ClaimsPrincipal available. In HTTP API context: ensure Authorization header is present. " +
            "In Blazor Server context: call BlazorPrincipalHolder.Set(authState.User) before Mediator.Send.");

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
