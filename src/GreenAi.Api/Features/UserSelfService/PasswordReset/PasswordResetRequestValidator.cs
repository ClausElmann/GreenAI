using FluentValidation;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public sealed class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequestCommand>
{
    public PasswordResetRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");
    }
}
