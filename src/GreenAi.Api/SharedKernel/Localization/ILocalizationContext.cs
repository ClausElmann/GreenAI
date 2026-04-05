namespace GreenAi.Api.SharedKernel.Localization;

/// <summary>
/// Scoped per-request/session localization context.
/// Loaded once with the user's language, then provides synchronous Get() for Blazor rendering.
/// Inject as: @inject ILocalizationContext Loc
/// Usage:     @Loc.Get("shared.SaveButton")
/// </summary>
public interface ILocalizationContext
{
    /// <summary>
    /// Returns the translated value for the key, with optional positional arguments.
    /// Placeholders use <c>string.Format</c> convention: <c>{0}</c>, <c>{1}</c>, etc.
    /// Returns <c>[?key?]</c> (or <c>[?key(args)?]</c>) if key is not found — never the raw key.
    /// </summary>
    /// <example>
    /// Loc.Get("shared.DeleteConfirm", customer.Name, count.ToString())
    /// // label: "Slet {0}? Det påvirker {1} profiler."
    /// </example>
    string Get(string key, params string[] args);

    /// <summary>
    /// Loads all labels for the given language. Call once in app layout or page OnInitializedAsync.
    /// Safe to call multiple times — only loads once.
    /// </summary>
    ValueTask EnsureLoadedAsync(int languageId, CancellationToken ct = default);
}
