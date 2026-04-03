using FluentValidation;

namespace GreenAi.Api.Features.Identity.ChangeUserEmail;

public sealed class ChangeUserEmailValidator : AbstractValidator<ChangeUserEmailCommand>
{
    public ChangeUserEmailValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.ConfirmNewEmail)
            .NotEmpty()
            .Equal(x => x.NewEmail)
            .WithMessage("Email addresses do not match.");
    }
}
