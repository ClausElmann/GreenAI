using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.ListSettings;

public static class ListSettingsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/settings", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListSettingsQuery(), ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("AdminLight");
    }
}
