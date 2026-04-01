using GreenAi.Api.Features.Auth.RefreshToken;
using MediatR;

namespace GreenAi.Api.Features.Auth.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", async (RefreshTokenCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error!.Message, statusCode: 401);
        });
    }
}
