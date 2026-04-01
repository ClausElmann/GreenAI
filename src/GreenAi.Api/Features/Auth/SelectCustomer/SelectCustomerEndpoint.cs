using GreenAi.Api.Features.Auth.SelectCustomer;
using MediatR;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public static class SelectCustomerEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/select-customer", async (SelectCustomerCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error!.Message, statusCode: 401);
        }).RequireAuthorization();
    }
}
