namespace GreenAi.Api.SharedKernel.Localization;

public interface ILocalizationRepository
{
    Task<string?> GetResourceValueAsync(string resourceName, int languageId, CancellationToken ct);
    Task<IReadOnlyDictionary<string, string>> GetAllResourcesAsync(int languageId, CancellationToken ct);
    Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken ct);
}
