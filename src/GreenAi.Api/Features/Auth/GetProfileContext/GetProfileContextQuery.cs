using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.GetProfileContext;

/// <summary>
/// Returns the display names for the active profile and customer.
/// Used by the UI shell (TopBar) to show contextual identity info.
/// No validator — parameterless query, identity resolved from ICurrentUser.
/// </summary>
public sealed record GetProfileContextQuery : IRequest<Result<GetProfileContextResponse>>;
