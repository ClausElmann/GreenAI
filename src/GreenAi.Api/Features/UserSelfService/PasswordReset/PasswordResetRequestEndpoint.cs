using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public static class PasswordResetRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/user/password-reset-request", async (
            PasswordResetRequestCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithTags("UserSelfService");
    }
}
