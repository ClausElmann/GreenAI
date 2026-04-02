using MediatR;

namespace GreenAi.Api.Features.Api.V1.Auth.Token;

public static class GetApiTokenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/token", async (
            GetApiTokenCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return result.Error!.Code switch
            {
                "ACCOUNT_LOCKED"      => Results.Problem(result.Error.Message, statusCode: 403),
                "INVALID_CREDENTIALS" => Results.Problem(result.Error.Message, statusCode: 401),
                _                     => Results.Problem(result.Error.Message, statusCode: 500)
            };
        })
        .WithTags("Public API v1");
    }
}
