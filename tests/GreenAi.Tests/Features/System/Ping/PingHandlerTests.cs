using GreenAi.Api.Features.System.Ping;

namespace GreenAi.Tests.Features.System.Ping;

public class PingHandlerTests
{
    [Fact]
    public async Task Handle_Always_ReturnsSuccessWithPong()
    {
        var handler = new PingHandler();

        var result = await handler.Handle(new PingQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("pong", result.Value!.Message);
    }
}
