using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Localization.BatchUpsertLabels;

public interface IBatchUpsertLabelsRepository
{
    Task UpsertAsync(LabelEntry label);
}

public sealed class BatchUpsertLabelsRepository : IBatchUpsertLabelsRepository
{
    private readonly IDbSession _db;

    public BatchUpsertLabelsRepository(IDbSession db) => _db = db;

    public Task UpsertAsync(LabelEntry label)
        => _db.ExecuteAsync(
            SqlLoader.Load<BatchUpsertLabelsRepository>("BatchUpsertLabels.sql"),
            new
            {
                label.ResourceName,
                label.ResourceValue,
                label.LanguageId
            });
}
