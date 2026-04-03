using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Identity.ChangeUserEmail;

public static class ChangeUserEmailEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/change-email", async (
            ChangeUserEmailCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("Identity");
    }
}
