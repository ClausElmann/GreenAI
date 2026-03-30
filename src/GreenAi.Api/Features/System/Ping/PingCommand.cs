using MediatR;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.Features.System.Ping;

public record PingCommand : IRequest<Result<PingResponse>>;
