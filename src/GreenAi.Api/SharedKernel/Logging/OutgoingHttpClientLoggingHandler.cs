namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// DelegatingHandler that logs all outgoing HTTP requests made via IHttpClientFactory.
///
/// Records: method, URI, status code, and duration for each outgoing call.
/// Errors (exceptions or 5xx) are logged at Warning level; all other calls at Debug.
///
/// Registration: add via HttpClientBuilder.AddHttpMessageHandler() or globally
/// via AddHttpClient().AddHttpMessageHandler&lt;OutgoingHttpClientLoggingHandler&gt;().
/// Must be registered as Transient in DI (DelegatingHandler requirement).
/// </summary>
public sealed class OutgoingHttpClientLoggingHandler : DelegatingHandler
{
    private readonly ILogger<OutgoingHttpClientLoggingHandler> _logger;

    public OutgoingHttpClientLoggingHandler(ILogger<OutgoingHttpClientLoggingHandler> logger)
        => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method.Method;
        var uri    = request.RequestUri?.ToString() ?? "(unknown)";
        var sw     = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            var status = (int)response.StatusCode;

            if (status >= 500)
                _logger.LogWarning("[HTTP-OUT] {Method} {Uri} → {Status} ({Elapsed}ms)", method, uri, status, sw.ElapsedMilliseconds);
            else
                _logger.LogDebug("[HTTP-OUT] {Method} {Uri} → {Status} ({Elapsed}ms)", method, uri, status, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[HTTP-OUT] {Method} {Uri} failed after {Elapsed}ms", method, uri, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
