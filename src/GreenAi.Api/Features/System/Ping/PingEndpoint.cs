using MediatR;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.Features.System.Ping;

public static class PingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ping", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new PingCommand());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error!.Message);
        });
    }
}
