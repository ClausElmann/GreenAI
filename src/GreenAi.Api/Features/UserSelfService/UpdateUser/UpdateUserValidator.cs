using FluentValidation;

namespace GreenAi.Api.Features.UserSelfService.UpdateUser;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x)
            .Must(x => x.DisplayName is not null || x.LanguageId is not null)
            .WithMessage("At least one field (DisplayName or LanguageId) must be provided.");

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .NotEmpty().WithMessage("DisplayName cannot be empty.")
                .MaximumLength(100).WithMessage("DisplayName must be 100 characters or fewer.");
        });

        When(x => x.LanguageId is not null, () =>
        {
            RuleFor(x => x.LanguageId!.Value)
                .GreaterThan(0).WithMessage("LanguageId must be greater than 0.");
        });
    }
}
