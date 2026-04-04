using GreenAi.Api.SharedKernel.Db;

namespace GreenAi.Api.SharedKernel.Settings;

/// <summary>
/// Implementerer IApplicationSettingService med fuld-load cache.
/// Cache-nøgle "Sms.applicationsettings" (bevaret for kompatibilitet med sms-service).
/// Cache invalideres ved Save().
/// </summary>
public sealed class ApplicationSettingService : IApplicationSettingService
{
    private readonly IDbSession _db;
    private Dictionary<AppSetting, string?>? _cache;

    public ApplicationSettingService(IDbSession db) => _db = db;

    public async Task<string?> GetAsync(AppSetting setting, string? defaultValue = null)
    {
        var all = await LoadCacheAsync();
        if (!all.TryGetValue(setting, out var value)) return defaultValue;
        return value ?? defaultValue;
    }

    public async Task SaveAsync(AppSetting setting, string? value)
    {
        var sql = SqlLoader.Load<ApplicationSettingService>("UpsertSetting.sql");
        await _db.ExecuteAsync(sql, new
        {
            TypeId = (int)setting,
            Name   = setting.ToString(),
            Value  = value
        });
        _cache = null; // invalidér
    }

    public async Task CreateDefaultsAsync()
    {
        // Indlæs eksisterende nøgler
        var existing = await LoadCacheAsync();

        // Opret manglende nøgler med null-value (tom streng)
        foreach (AppSetting setting in Enum.GetValues<AppSetting>())
        {
            if (!existing.ContainsKey(setting))
                await SaveAsync(setting, null);
        }

        // Invalidér cache så næste kald læser alle nøgler inkl. de nye
        _cache = null;
    }

    // -----------------------------------------------------------------------

    private async Task<Dictionary<AppSetting, string?>> LoadCacheAsync()
    {
        if (_cache is not null)
            return _cache;

        var sql = SqlLoader.Load<ApplicationSettingService>("GetAllSettings.sql");
        var rows = await _db.QueryAsync<ApplicationSettingRow>(sql);

        _cache = rows
            .Where(r => Enum.IsDefined(typeof(AppSetting), r.ApplicationSettingTypeId))
            .ToDictionary(
                r => (AppSetting)r.ApplicationSettingTypeId,
                r => r.Value);

        return _cache;
    }

    private sealed record ApplicationSettingRow(
        int ApplicationSettingTypeId,
        string? Value);
}
