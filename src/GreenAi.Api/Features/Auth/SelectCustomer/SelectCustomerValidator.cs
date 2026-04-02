using FluentValidation;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public sealed class SelectCustomerValidator : AbstractValidator<SelectCustomerCommand>
{
    public SelectCustomerValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("CustomerId must be a valid positive integer.");
    }
}
