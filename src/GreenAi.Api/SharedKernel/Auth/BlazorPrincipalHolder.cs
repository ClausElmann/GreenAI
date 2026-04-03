using System.Security.Claims;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Scoped service that bridges Blazor Server circuits (WebSocket) and MediatR handlers.
///
/// Blazor Server components run on a SignalR circuit, not a standard HTTP request.
/// IHttpContextAccessor.HttpContext is null inside component lifecycle methods (OnInitializedAsync, etc.)
/// which means HttpContextCurrentUser cannot read JWT claims from the request.
///
/// Usage in Blazor pages:
///   1. Inject BlazorPrincipalHolder and AuthenticationStateProvider (or use [CascadingParameter])
///   2. Call Set(authState.User) BEFORE any Mediator.Send(...) call
///   Then ICurrentUser → HttpContextCurrentUser → falls back to this holder automatically.
///
/// HTTP API endpoints and integration tests are unaffected — IHttpContextAccessor.HttpContext
/// is always available there, so the fallback is never reached.
/// </summary>
public sealed class BlazorPrincipalHolder
{
    private ClaimsPrincipal? _principal;

    /// <summary>Set the authenticated principal for the current Blazor circuit.</summary>
    public void Set(ClaimsPrincipal principal) => _principal = principal;

    /// <summary>Returns the principal set by the current Blazor circuit, or null if not set.</summary>
    public ClaimsPrincipal? Current => _principal;
}
