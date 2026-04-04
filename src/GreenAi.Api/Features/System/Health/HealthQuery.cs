using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.System.Health;

public record HealthQuery : IRequest<Result<HealthResponse>>;
