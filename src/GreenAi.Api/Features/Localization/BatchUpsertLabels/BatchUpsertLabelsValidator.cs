using FluentValidation;

namespace GreenAi.Api.Features.Localization.BatchUpsertLabels;

public sealed class BatchUpsertLabelsValidator : AbstractValidator<BatchUpsertLabelsCommand>
{
    private const int MaxBatchSize = 500;

    public BatchUpsertLabelsValidator()
    {
        RuleFor(x => x.Labels)
            .NotNull()
            .Must(labels => labels.Count <= MaxBatchSize)
            .WithMessage($"Batch size cannot exceed {MaxBatchSize} labels per request.");

        RuleForEach(x => x.Labels).ChildRules(label =>
        {
            label.RuleFor(x => x.ResourceName)
                .NotEmpty()
                .MaximumLength(200);

            label.RuleFor(x => x.ResourceValue)
                .NotNull()
                .MaximumLength(2000);

            label.RuleFor(x => x.LanguageId)
                .GreaterThan(0);
        });
    }
}
