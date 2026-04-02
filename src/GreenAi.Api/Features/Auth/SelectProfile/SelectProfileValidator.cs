using FluentValidation;

namespace GreenAi.Api.Features.Auth.SelectProfile;

public sealed class SelectProfileValidator : AbstractValidator<SelectProfileCommand>
{
    public SelectProfileValidator()
    {
        // ProfileId is optional — null signals auto-select.
        // When provided, must be a positive integer.
        RuleFor(x => x.ProfileId)
            .GreaterThan(0)
            .When(x => x.ProfileId.HasValue)
            .WithMessage("ProfileId must be a valid positive integer when provided.");
    }
}
