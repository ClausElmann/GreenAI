namespace GreenAi.Api.SharedKernel.Settings;

/// <summary>
/// Læser og gemmer systemkonfiguration fra ApplicationSettings-tabellen.
/// Full-load cache — alle nøgler indlæses ved første kald og cachet.
/// Cache invalideres ved Save().
/// </summary>
public interface IApplicationSettingService
{
    /// <summary>
    /// Henter en indstilling. Returnerer defaultValue hvis nøglen ikke eksisterer i DB.
    /// Opretter IKKE automatisk rækken i denne variant — brug CreateDefaults() ved opstart.
    /// </summary>
    Task<string?> GetAsync(AppSetting setting, string? defaultValue = null);

    /// <summary>
    /// Gemmer (upsert) en indstilling og invaliderer cachen.
    /// </summary>
    Task SaveAsync(AppSetting setting, string? value);

    /// <summary>
    /// Opretter standardrækker for alle nøgler der endnu ikke eksisterer i DB.
    /// Idempotent — sikkert at kalde ved app-start.
    /// </summary>
    Task CreateDefaultsAsync();
}
