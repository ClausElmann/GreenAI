using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.UpdateUser;

public static class UpdateUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/user/update", async (
            UpdateUserCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("UserSelfService");
    }
}
