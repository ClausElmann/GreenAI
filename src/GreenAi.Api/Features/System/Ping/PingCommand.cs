using MediatR;
using GreenAi.Api.SharedKernel.Results;

namespace GreenAi.Api.Features.System.Ping;

public record PingQuery : IRequest<Result<PingResponse>>;
