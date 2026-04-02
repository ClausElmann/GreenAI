namespace GreenAi.Api.SharedKernel.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly ILocalizationRepository _repository;

    public LocalizationService(ILocalizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GetAsync(string resourceName, int languageId, CancellationToken ct = default)
    {
        var value = await _repository.GetResourceValueAsync(resourceName, languageId, ct);
        // Fail-open: return the key itself if no translation found — never null/empty
        return value ?? resourceName;
    }

    public async Task<string> GetAsync(string resourceName, int languageId, IDictionary<string, string> parameters, CancellationToken ct = default)
    {
        var value = await GetAsync(resourceName, languageId, ct);

        foreach (var (token, replacement) in parameters)
            value = value.Replace($"{{{token}}}", replacement, StringComparison.Ordinal);

        return value;
    }

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(int languageId, CancellationToken ct = default)
        => _repository.GetAllResourcesAsync(languageId, ct);

    public Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken ct = default)
        => _repository.GetLanguagesAsync(ct);
}
