using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

/// <summary>
/// Requests a password reset for the specified email address.
/// Anonymous — no authentication required.
/// Always returns success to prevent email enumeration.
/// </summary>
public sealed record PasswordResetRequestCommand(string Email)
    : IRequest<Result<PasswordResetRequestResponse>>;
