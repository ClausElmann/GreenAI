using System.Security.Claims;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Gates all authenticated /api/ requests that are NOT auth-route exemptions.
///
/// Enforces C_001 + C_005:
///   - customerId = 0 → HTTP 401 (no tenant context — must call select-customer first)
///   - profileId  = 0 → HTTP 401 (no profile context — must call select-profile first)
///
/// Auth routes are exempt because they are meant to be called while profileId/customerId=0:
///   /api/auth/* — login, select-customer, select-profile, refresh, logout
///
/// Infrastructure routes are not authenticated so the middleware never triggers:
///   /api/ping, /api/health, /api/client-log
///
/// Design note: this runs at the HTTP middleware level, not the MediatR pipeline level.
/// RequireProfileBehavior covers the MediatR layer for IRequireProfile requests.
/// This middleware is the HTTP-level early gate — returns before routing, before MediatR.
/// </summary>
public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (IsProtectedApiRoute(ctx.Request.Path) && ctx.User.Identity?.IsAuthenticated == true)
        {
            var customerIdClaim = ctx.User.FindFirstValue(GreenAiClaims.CustomerId);
            var profileIdClaim  = ctx.User.FindFirstValue(GreenAiClaims.ProfileId);

            var customerOk = int.TryParse(customerIdClaim, out var cid) && cid > 0;
            var profileOk  = int.TryParse(profileIdClaim,  out var pid) && pid > 0;

            if (!customerOk || !profileOk)
            {
                ctx.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsync(
                    """{"status":401,"title":"Incomplete authentication context","detail":"Call /api/auth/select-customer and /api/auth/select-profile before accessing this resource."}""");
                return;
            }
        }

        await _next(ctx);
    }

    // Protected: /api/ routes that are NOT /api/auth/*
    private static bool IsProtectedApiRoute(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase);
}
