namespace GreenAi.E2E.Visual;

/// <summary>
/// Viewport presets for multi-device visual testing.
/// Matches the device matrix in docs/SSOT/ui/greenai-ui-architecture.md.
///
/// Add new profiles here when new breakpoints need coverage.
/// </summary>
public sealed record DeviceProfile(
    string Name,
    int    Width,
    int    Height,
    bool   IsMobile = false)
{
    /// <summary>All device profiles exercised by visual tests.</summary>
    public static readonly IReadOnlyList<DeviceProfile> All =
    [
        new("Desktop", 1920, 1080),
        new("Laptop",  1366, 768),
        new("Tablet",  1024, 768),
        new("Mobile",   390, 844, IsMobile: true),
    ];

    /// <summary>Filesystem-safe folder name: "Desktop" → "desktop", "iPad Pro" → "ipad_pro".</summary>
    public string FolderName =>
        string.Concat(Name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_'));
}
