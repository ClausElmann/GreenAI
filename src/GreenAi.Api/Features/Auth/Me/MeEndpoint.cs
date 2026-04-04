using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Me;

public static class MeEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new MeQuery(), ct);
            return result.ToHttpResult();
        }).RequireAuthorization();
    }
}
