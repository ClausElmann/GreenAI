using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.System.Health;

public static class HealthEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new HealthQuery());
            return result.ToHttpResult();
        });
    }
}
