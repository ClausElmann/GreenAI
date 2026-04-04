using FluentValidation;

namespace GreenAi.Api.Features.AdminLight.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("Initial password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(512).WithMessage("Password must be 512 characters or fewer.");

        RuleFor(x => x.LanguageId)
            .GreaterThan(0).WithMessage("LanguageId must be greater than 0.");
    }
}
