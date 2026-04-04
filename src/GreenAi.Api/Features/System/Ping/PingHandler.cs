using MediatR;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.Features.System.Ping;

public sealed class PingHandler : IRequestHandler<PingQuery, Result<PingResponse>>
{
    public Task<Result<PingResponse>> Handle(PingQuery request, CancellationToken cancellationToken)
    {
        var response = new PingResponse("pong", DateTimeOffset.UtcNow);
        return Task.FromResult(Result<PingResponse>.Ok(response));
    }
}
