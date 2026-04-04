using MediatR;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.Features.System.Ping;

public static class PingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ping", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new PingQuery());
            return result.ToHttpResult();
        });
    }
}
