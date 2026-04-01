using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.SelectProfile;

/// <summary>
/// Selects one of the user's accessible profiles for the active customer and issues a JWT
/// with a real ProfileId > 0.
///
/// ProfileId == null → auto-select if exactly one profile is accessible.
/// ProfileId is provided → validate access and select explicitly.
/// Multiple profiles with no selection → returns NeedsProfileSelection without issuing a JWT.
/// </summary>
public sealed record SelectProfileCommand(int? ProfileId)
    : IRequest<Result<SelectProfileResponse>>, IRequireAuthentication;
