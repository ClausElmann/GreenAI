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
    /// Returns the translated value for the key. Returns the key itself if not found (fail-open).
    /// </summary>
    string Get(string key);

    /// <summary>
    /// Returns the translated value with {0} replaced by the provided argument.
    /// </summary>
    string Get(string key, string arg0);

    /// <summary>
    /// Loads all labels for the given language. Call once in app layout or page OnInitializedAsync.
    /// Safe to call multiple times — only loads once.
    /// </summary>
    ValueTask EnsureLoadedAsync(int languageId, CancellationToken ct = default);
}
