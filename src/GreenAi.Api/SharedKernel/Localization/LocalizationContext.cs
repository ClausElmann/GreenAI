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

    public string Get(string key, params string[] args)
    {
        if (!_labels.TryGetValue(key.ToUpperInvariant(), out var value))
        {
            // Never silently return the raw key — [?key?] is immediately visible and
            // searchable in browser DevTools. Include args for debuggability.
            return args.Length > 0
                ? $"[?{key}({string.Join(", ", args)})?]"
                : $"[?{key}?]";
        }

        // string.Format convention: {0}, {1}, {2} — same as every .NET developer expects
        for (var i = 0; i < args.Length; i++)
            value = value.Replace($"{{{i}}}", args[i], StringComparison.Ordinal);

        return value;
    }
}
