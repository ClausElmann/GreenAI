using GreenAi.Api.SharedKernel.Db;

namespace GreenAi.Api.SharedKernel.Localization;

public sealed class LocalizationRepository : ILocalizationRepository
{
    private readonly IDbSession _db;

    public LocalizationRepository(IDbSession db)
    {
        _db = db;
    }

    public async Task<string?> GetResourceValueAsync(string resourceName, int languageId, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 [ResourceValue]
            FROM [dbo].[Labels]
            WHERE [LanguageId] = @LanguageId
              AND [ResourceName] = @ResourceName
            """;

        return await _db.QuerySingleOrDefaultAsync<string>(sql, new { LanguageId = languageId, ResourceName = resourceName });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllResourcesAsync(int languageId, CancellationToken ct)
    {
        const string sql = """
            SELECT [ResourceName], [ResourceValue]
            FROM [dbo].[Labels]
            WHERE [LanguageId] = @LanguageId
            ORDER BY [ResourceName]
            """;

        var rows = await _db.QueryAsync<(string ResourceName, string ResourceValue)>(sql, new { LanguageId = languageId });

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in rows)
        {
            result.TryAdd(name, value);
        }

        return result;
    }

    public async Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT [Id], [Name], [LanguageCulture], [UniqueSeoCode], [Published], [DisplayOrder]
            FROM [dbo].[Languages]
            WHERE [Published] = 1
            ORDER BY [DisplayOrder], [Id]
            """;

        var rows = await _db.QueryAsync<Language>(sql);
        return rows.ToList();
    }
}
