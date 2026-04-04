using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.AssignProfile;

public static class AssignProfileEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/users/{id}/profiles", async (
            int id,
            AssignProfileRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new AssignProfileCommand(id, request.ProfileId);
            var result  = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("AdminLight");
    }
}

public sealed record AssignProfileRequest(int ProfileId);
