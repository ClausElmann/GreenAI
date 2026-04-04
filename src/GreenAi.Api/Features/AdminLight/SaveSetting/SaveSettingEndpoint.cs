using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.SaveSetting;

public static class SaveSettingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/settings/{key:int}", async (
            int key,
            SaveSettingRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new SaveSettingCommand(key, request.Value);
            var result  = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("AdminLight");
    }
}

public sealed record SaveSettingRequest(string? Value);
