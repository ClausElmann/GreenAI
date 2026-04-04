using FluentValidation;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public sealed class PasswordResetConfirmValidator : AbstractValidator<PasswordResetConfirmCommand>
{
    public PasswordResetConfirmValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .Length(64).WithMessage("Token must be exactly 64 characters.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(512).WithMessage("Password must be 512 characters or fewer.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
