using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Api.V1.Auth.Token;

/// <summary>
/// Issues a JWT access token for programmatic (machine-to-machine) API access.
/// The caller must know their CustomerId and ProfileId — no interactive selection flow.
/// The user account must have the UserRole "API" assigned.
/// </summary>
public record GetApiTokenCommand(
    string Email,
    string Password,
    int CustomerId,
    int ProfileId)
    : IRequest<Result<GetApiTokenResponse>>;
