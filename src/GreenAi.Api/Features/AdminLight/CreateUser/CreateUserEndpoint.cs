using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.AdminLight.CreateUser;

public static class CreateUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/users", async (
            CreateUserCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("AdminLight");
    }
}
