using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/auth/logout", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LogoutCommand(), ct);
            return result.IsSuccess
                ? Microsoft.AspNetCore.Http.Results.NoContent()
                : result.ToHttpResult();
        }).RequireAuthorization();
    }
}
