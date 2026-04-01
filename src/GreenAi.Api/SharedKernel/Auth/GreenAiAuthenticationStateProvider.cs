using GreenAi.Api.Features.Auth.RefreshToken;
using GreenAi.Api.SharedKernel.Auth;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Reads JWT from localStorage on first render.
/// Starts a background timer that refreshes the token 2 minutes before expiry.
/// Disposes the timer on circuit disconnect.
/// </summary>
public sealed class GreenAiAuthenticationStateProvider : AuthenticationStateProvider, IAsyncDisposable
{
    private const string AccessTokenKey = "greenai_access_token";
    private const string RefreshTokenKey = "greenai_refresh_token";
    private const string ExpiresAtKey = "greenai_expires_at";

    private readonly IJSRuntime _js;
    private readonly IMediator _mediator;
    private readonly JwtTokenService _jwt;

    private Timer? _refreshTimer;

    public GreenAiAuthenticationStateProvider(
        IJSRuntime js,
        IMediator mediator,
        JwtTokenService jwt)
    {
        _js = js;
        _mediator = mediator;
        _jwt = jwt;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var accessToken = await _js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
            if (string.IsNullOrWhiteSpace(accessToken))
                return Unauthenticated();

            var principal = _jwt.ValidateToken(accessToken);
            if (principal is null)
            {
                await ClearStorageAsync();
                return Unauthenticated();
            }

            ScheduleRefresh(principal);
            return new AuthenticationState(principal);
        }
        catch
        {
            // JS not available (prerender) — return unauthenticated
            return Unauthenticated();
        }
    }

    public async Task SignInAsync(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
        await _js.InvokeVoidAsync("localStorage.setItem", ExpiresAtKey, expiresAt.ToString("O"));

        var principal = _jwt.ValidateToken(accessToken);
        if (principal is null) return;

        ScheduleRefresh(principal);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task SignOutAsync()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
        await ClearStorageAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Unauthenticated()));
    }

    private void ScheduleRefresh(ClaimsPrincipal principal)
    {
        _refreshTimer?.Dispose();

        var expiryClaim = principal.FindFirstValue("exp");
        if (expiryClaim is null || !long.TryParse(expiryClaim, out var expUnix))
            return;

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
        var refreshAt = expiresAt.AddMinutes(-2);
        var delay = refreshAt - DateTimeOffset.UtcNow;

        if (delay <= TimeSpan.Zero)
        {
            _ = TryRefreshAsync();
            return;
        }

        _refreshTimer = new Timer(_ => _ = TryRefreshAsync(), null, delay, Timeout.InfiniteTimeSpan);
    }

    private async Task TryRefreshAsync()
    {
        try
        {
            var refreshToken = await _js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await SignOutAsync();
                return;
            }

            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));
            if (!result.IsSuccess)
            {
                await SignOutAsync();
                return;
            }

            await SignInAsync(result.Value!.AccessToken, result.Value.RefreshToken, result.Value.ExpiresAt);
        }
        catch
        {
            await SignOutAsync();
        }
    }

    private async Task ClearStorageAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", ExpiresAtKey);
    }

    private static AuthenticationState Unauthenticated()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    public async ValueTask DisposeAsync()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }
}
