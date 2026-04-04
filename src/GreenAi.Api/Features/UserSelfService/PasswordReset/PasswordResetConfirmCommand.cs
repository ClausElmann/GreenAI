using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

/// <summary>
/// Validates a password reset token and sets a new password.
/// Anonymous — no authentication required (user cannot authenticate without their password).
/// </summary>
public sealed record PasswordResetConfirmCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword)
    : IRequest<Result<PasswordResetConfirmResponse>>;
