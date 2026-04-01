using MediatR;

namespace GreenAi.Api.Features.Auth.SelectProfile;

public static class SelectProfileEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/select-profile", async (SelectProfileCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error!.Message, statusCode: 401);
        }).RequireAuthorization();
    }
}
