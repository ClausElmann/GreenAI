using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.AssignRole;

public static class AssignRoleEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/users/{id}/roles", async (
            int id,
            AssignRoleRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new AssignRoleCommand(id, request.RoleName);
            var result  = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("AdminLight");
    }
}

public sealed record AssignRoleRequest(string RoleName);
