namespace GreenAi.Api.SharedKernel.Settings;

/// <summary>
/// Repræsenterer én konfigurationsnøgle fra ApplicationSettings-tabellen.
/// </summary>
public sealed record ApplicationSetting(
    int Id,
    AppSetting SettingType,
    string Name,
    string? Value,
    DateTimeOffset UpdatedAt);
