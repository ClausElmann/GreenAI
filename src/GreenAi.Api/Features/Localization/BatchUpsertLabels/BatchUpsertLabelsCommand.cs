using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Localization.BatchUpsertLabels;

/// <summary>A single label row to upsert (insert or update).</summary>
public record LabelEntry(string ResourceName, string ResourceValue, int LanguageId);

public record BatchUpsertLabelsCommand(IReadOnlyList<LabelEntry> Labels)
    : IRequest<Result<BatchUpsertLabelsResponse>>;
