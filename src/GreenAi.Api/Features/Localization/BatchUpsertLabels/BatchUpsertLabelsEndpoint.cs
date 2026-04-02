using GreenAi.Api.Features.Localization.BatchUpsertLabels;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Localization.BatchUpsertLabels;

public static class BatchUpsertLabelsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/labels/batch-upsert", async (
            BatchUpsertLabelsCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("Localization");
    }
}
