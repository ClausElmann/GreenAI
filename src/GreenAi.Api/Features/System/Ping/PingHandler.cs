using MediatR;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.Features.System.Ping;

public sealed class PingHandler : IRequestHandler<PingCommand, Result<PingResponse>>
{
    public Task<Result<PingResponse>> Handle(PingCommand request, CancellationToken cancellationToken)
    {
        var response = new PingResponse("pong", DateTimeOffset.UtcNow);
        return Task.FromResult(Result<PingResponse>.Ok(response));
    }
}
