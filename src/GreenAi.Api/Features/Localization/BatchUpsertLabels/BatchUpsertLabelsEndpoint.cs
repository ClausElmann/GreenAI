using GreenAi.Api.Features.Localization.BatchUpsertLabels;
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
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return result.Error!.Code switch
            {
                "UNAUTHORIZED" => Results.Unauthorized(),
                "FORBIDDEN"    => Results.Forbid(),
                _              => Results.Problem(result.Error.Message, statusCode: 500)
            };
        })
        .RequireAuthorization()
        .WithTags("Localization");
    }
}
