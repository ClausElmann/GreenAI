namespace GreenAi.Api.SharedKernel.Localization;

public interface ILocalizationService
{
    /// <summary>
    /// Look up a translation by key and language.
    /// Returns the key itself if no match — fail-open by design (never null/empty).
    /// </summary>
    Task<string> GetAsync(string resourceName, int languageId, CancellationToken ct = default);

    /// <summary>
    /// Look up a translation and replace {tokens} with provided values.
    /// </summary>
    Task<string> GetAsync(string resourceName, int languageId, IDictionary<string, string> parameters, CancellationToken ct = default);

    /// <summary>
    /// Returns all labels for a language as a flat dictionary (UPPER-cased keys).
    /// Used for bootstrap endpoint consumed by frontend at startup.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(int languageId, CancellationToken ct = default);

    /// <summary>
    /// Returns all languages.
    /// </summary>
    Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken ct = default);
}
