using GreenAi.Api.SharedKernel.Settings;

namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// Middleware that logs HTTP request/response pairs with body snippets.
///
/// Controlled by AppSetting.RequestLogLevel (read at runtime per request — hot-reload safe):
///   "off"   → no logging (default)
///   "error" → log only responses with status >= 400
///   "all"   → log all requests
///
/// Bodies are truncated at 4 KB to avoid flooding the log table.
/// Responses are never buffered for inspection when logging is off.
///
/// Position in pipeline: after UseAuthentication + UseAuthorization so that UserId
/// is available from ICurrentUser when the log entry is written.
/// </summary>
public sealed class RequestResponseLoggingMiddleware
{
    private const int MaxBodyBytes = 4096;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve IApplicationSettingService from the request scope — it is Scoped,
        // so it cannot be injected into the middleware constructor (which is Singleton-scoped).
        var settings = context.RequestServices.GetRequiredService<IApplicationSettingService>();

        var level = (await settings.GetAsync(AppSetting.RequestLogLevel, "off") ?? "off")
            .ToLowerInvariant();

        if (level == "off")
        {
            await _next(context);
            return;
        }

        // Capture the response body so we can read status after the pipeline runs.
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            var status = context.Response.StatusCode;
            var shouldLog = level == "all" || (level == "error" && status >= 400);

            if (shouldLog)
            {
                var method = context.Request.Method;
                var path   = context.Request.Path.Value ?? string.Empty;
                var query  = context.Request.QueryString.Value ?? string.Empty;

                buffer.Seek(0, SeekOrigin.Begin);
                var bodyBytes  = Math.Min((int)buffer.Length, MaxBodyBytes);
                var bodySnippet = bodyBytes > 0
                    ? System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), 0, bodyBytes)
                    : string.Empty;

                _logger.LogInformation(
                    "[REQ] {Method} {Path}{Query} → {Status} | body[0..{BodyLen}]: {BodySnippet}",
                    method, path, query, status, bodyBytes, bodySnippet);
            }

            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }
}
