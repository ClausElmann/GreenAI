using System.Net;
using GreenAi.Api.SharedKernel.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GreenAi.Tests.SharedKernel.Logging;

/// <summary>
/// Unit tests for OutgoingHttpClientLoggingHandler.
///
/// Verifies that the handler passes through responses correctly and logs at the
/// appropriate level based on status code.
/// </summary>
public sealed class OutgoingHttpClientLoggingHandlerTests
{
    private static (HttpClient Client, ILogger<OutgoingHttpClientLoggingHandler> Logger)
        CreateClient(HttpStatusCode responseStatus)
    {
        var logger = Substitute.For<ILogger<OutgoingHttpClientLoggingHandler>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var handler = new OutgoingHttpClientLoggingHandler(logger)
        {
            InnerHandler = new StaticResponseHandler(responseStatus)
        };

        return (new HttpClient(handler), logger);
    }

    [Fact]
    public async Task Send_200Response_ReturnsSuccessfully()
    {
        var (client, _) = CreateClient(HttpStatusCode.OK);

        var response = await client.GetAsync("https://example.test/ping", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Send_200Response_LogsAtDebugLevel()
    {
        var (client, logger) = CreateClient(HttpStatusCode.OK);

        await client.GetAsync("https://example.test/ping", TestContext.Current.CancellationToken);

        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Send_500Response_LogsAtWarningLevel()
    {
        var (client, logger) = CreateClient(HttpStatusCode.InternalServerError);

        await client.GetAsync("https://example.test/fail", TestContext.Current.CancellationToken);

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Send_404Response_LogsAtDebugLevel()
    {
        // 404 is a client error but not a server error — logged at Debug (not Warning)
        var (client, logger) = CreateClient(HttpStatusCode.NotFound);

        await client.GetAsync("https://example.test/missing", TestContext.Current.CancellationToken);

        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    // -------------------------------------------------------------------------

    private sealed class StaticResponseHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status));
    }
}
