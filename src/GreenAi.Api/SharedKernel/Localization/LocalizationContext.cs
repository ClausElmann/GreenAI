namespace GreenAi.Api.SharedKernel.Localization;

public sealed class LocalizationContext : ILocalizationContext
{
    private readonly ILocalizationService _service;
    private IReadOnlyDictionary<string, string> _labels = new Dictionary<string, string>();
    private bool _loaded;

    public LocalizationContext(ILocalizationService service)
    {
        _service = service;
    }

    public async ValueTask EnsureLoadedAsync(int languageId, CancellationToken ct = default)
    {
        if (_loaded) return;
        _labels = await _service.GetAllAsync(languageId, ct);
        _loaded = true;
    }

    public string Get(string key)
        => _labels.TryGetValue(key.ToUpperInvariant(), out var value) ? value : key;

    public string Get(string key, string arg0)
        => Get(key).Replace("{0}", arg0, StringComparison.Ordinal);
}
