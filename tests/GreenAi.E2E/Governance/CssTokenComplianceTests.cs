using System.Text.RegularExpressions;

namespace GreenAi.E2E.Governance;

/// <summary>
/// Static source-code governance tests — no browser required.
///
/// <list type="bullet">
///   <item>CssTokenComplianceTests — fail if hardcoded colour/spacing values
///         are found in any our own .css source file instead of --ga-* tokens.</item>
/// </list>
///
/// These run fast (&lt;100ms) and are always included regardless of GREENAI_* env vars.
/// </summary>
public sealed class CssTokenComplianceTests
{
    // ── Source root (relative to test binary — three levels up from bin/Debug/net10.0/) ──
    private static readonly string SourceRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GreenAi.Api"));

    // ── CSS files owned by us (never examine third-party) ────────────────────
    private static string[] OurCssFiles() =>
        Directory.GetFiles(SourceRoot, "*.css", SearchOption.AllDirectories)
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                // Exclude Blazor build outputs, build intermediates, and third-party
                return !name.StartsWith("bootstrap", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("MudBlazor", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains(".styles.css")
                    && !name.Contains(".scp.css")
                    && !name.Contains(".bundle.scp.css")
                    && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar);
            })
            .ToArray();

    // ── Patterns that are ALLOWED even though they look like hardcoded values ─
    // - Colours inside CSS custom property definitions (the :root block)
    // - Shadow values using rgba(16,24,40, ...) — our shadow-token values
    // - Primary-specific rgba (rgba(11,95,255,...)) — used for tints
    // - White/black rgba (rgba(0,0,0,...), rgba(255,255,255,...)) — shadows/overlays
    // - data: URIs (SVG icons inline)
    // - gradient() functions
    private static readonly Regex AllowedPattern = new Regex(
        @"--ga-|--mud-|--bs-|var\(|data:|base64|gradient|" +
        @"rgba\(1[56],2[34],4[01]|" +  // rgba(16,24,40 / rgba(15,23,42 shadows
        @"rgba\(11,\s*9[45]|" +         // rgba(11,95,255 primary tints
        @"rgba\(0,\s*0,\s*0|" +         // rgba(0,0,0 shadows
        @"rgba\(25[45],\s*25[45]|" +    // rgba(254/255,255/254 off-white
        @"rgba\(17,\s*2[234]|" +        // rgba(17,24,39 focus halo
        @"rgba\(24[0-9]|" +             // rgba(241,... surface-alt tints
        @"rgba\(75,\s*91|" +            // rgba(75,91,107 status-draft
        @"rgba\(16[0-1],\s*9[02]|" +    // rgba(161,92,0 warning
        @"rgba\(18[0-1],\s*3[45]|" +    // rgba(180,35,24 danger
        @"rgba\(0,\s*9[4-9]|" +         // rgba(0,94,122 info
        @"rgba\(1[0-9],\s*12[02]",      // rgba(17,122,55 success-dark
        RegexOptions.Compiled);

    // ── The actual failing pattern ────────────────────────────────────────────
    // Matches hex colours (#RGB, #RRGGBB) and bare rgb()/rgba() NOT covered by AllowedPattern
    private static readonly Regex HardcodedColor = new Regex(
        @"(?<![a-zA-Z0-9_-])#[0-9a-fA-F]{3,8}\b|rgba?\(\d",
        RegexOptions.Compiled);

    // ── Allowed spacing exceptions (px values that are intentional in source) ──
    // These are specific well-known intentional values: 0, pixel-perfect tuning
    private static readonly HashSet<double> AllowedSpacingPx =
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 20, 24, 28, 32, 36, 40, 44, 48, 56, 64, 72, 80, 88, 100];

    [Fact]
    [Trait("Category", "Governance")]
    public void NoCssSourceFiles_ContainHardcodedColorValues()
    {
        var cssFiles = OurCssFiles();
        Assert.True(cssFiles.Length > 0, $"No own CSS files found under {SourceRoot} — path may be wrong.");

        var violations = new List<string>();

        foreach (var file in cssFiles)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // Skip comment lines
                if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("*") || line.TrimStart().StartsWith("/*"))
                    continue;

                var matches = HardcodedColor.Matches(line);
                foreach (Match m in matches)
                {
                    // Check position in line — value must NOT be inside a CSS variable definition
                    var beforeMatch = line[..m.Index];
                    if (beforeMatch.Contains("--ga-") || beforeMatch.Contains("--mud-"))
                        continue; // it's the value of a token definition — allowed

                    if (AllowedPattern.IsMatch(line))
                        continue; // line contains an allowed context (token var, shadow rgba, etc.)

                    violations.Add($"{Path.GetFileName(file)}:{i + 1}  {line.Trim()}");
                }
            }
        }

        if (violations.Count > 0)
        {
            var msg = string.Join("\n", violations.Take(20).Select(v => $"  • {v}"));
            Assert.Fail(
                $"Hardcoded colour values found in {violations.Count} CSS line(s). " +
                $"Use --ga-* design tokens instead:\n{msg}");
        }
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void CssSourceFiles_ExistAndAreReachable()
    {
        var cssFiles = OurCssFiles();
        Assert.True(cssFiles.Length >= 3,
            $"Expected at least 3 own CSS files under {SourceRoot}, found {cssFiles.Length}. " +
            "CssTokenComplianceTests path resolution may be broken.");

        // The two core files must always be present
        Assert.Contains(cssFiles, f => f.EndsWith("app.css"));
        Assert.Contains(cssFiles, f => f.EndsWith("greenai-skin.css"));
        Assert.Contains(cssFiles, f => f.EndsWith("greenai-enterprise.css"));
    }
}
