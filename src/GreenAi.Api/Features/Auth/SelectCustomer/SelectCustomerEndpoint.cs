using GreenAi.Api.Features.Auth.SelectCustomer;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public static class SelectCustomerEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/select-customer", async (SelectCustomerCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.ToHttpResult();
        }).RequireAuthorization();
    }
}
