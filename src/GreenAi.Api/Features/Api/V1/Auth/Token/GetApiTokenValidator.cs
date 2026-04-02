using FluentValidation;

namespace GreenAi.Api.Features.Api.V1.Auth.Token;

public sealed class GetApiTokenValidator : AbstractValidator<GetApiTokenCommand>
{
    public GetApiTokenValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.CustomerId)
            .GreaterThan(0);

        RuleFor(x => x.ProfileId)
            .GreaterThan(0);
    }
}
