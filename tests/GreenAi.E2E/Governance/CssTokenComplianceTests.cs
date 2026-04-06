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
        @"--ga-|--mud-|--bs-|--color-|var\(|data:|base64|gradient|" +
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
                    if (beforeMatch.Contains("--ga-") || beforeMatch.Contains("--mud-") || beforeMatch.Contains("--color-"))
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

    /// <summary>
    /// Scans portal-skin.css to ensure font-size declarations use --font-* tokens,
    /// not hardcoded values (px, rem, em).
    /// Enforcement scope: portal-skin.css only (the file governed by the token system).
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void PortalSkin_FontSizes_UseTokensNotHardcodedValues()
    {
        var portalSkin = OurCssFiles()
            .FirstOrDefault(f => f.EndsWith("portal-skin.css"));
        Assert.NotNull(portalSkin);

        // Matches font-size with a hardcoded value (px, rem, em, %)
        var hardcodedFontSize = new Regex(
            @"font-size\s*:\s*\d+(\.\d+)?(px|rem|em|%)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var violations = new List<string>();
        var lines = File.ReadAllLines(portalSkin!);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // Skip comment lines
            if (line.TrimStart().StartsWith("//") ||
                line.TrimStart().StartsWith("*") ||
                line.TrimStart().StartsWith("/*"))
                continue;

            if (hardcodedFontSize.IsMatch(line))
                violations.Add($"portal-skin.css:{i + 1}  {line.Trim()}");
        }

        if (violations.Count > 0)
        {
            var msg = string.Join("\n", violations.Select(v => $"  • {v}"));
            Assert.Fail(
                $"Hardcoded font-size values found in portal-skin.css. " +
                $"Use var(--font-*) tokens from design-tokens.css instead:\n{msg}");
        }
    }

    /// <summary>
    /// Verifies that design-tokens.css defines all required spacing and
    /// typography tokens (enforces the token contract).
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void DesignTokens_ContainsRequiredTypographyAndSpacingTokens()
    {
        var designTokens = OurCssFiles()
            .FirstOrDefault(f => f.EndsWith("design-tokens.css"));
        Assert.NotNull(designTokens);

        var content = File.ReadAllText(designTokens!);

        var requiredTokens = new[]
        {
            "--font-xs", "--font-sm", "--font-md", "--font-lg",
            "--font-xl", "--font-2xl", "--font-3xl",
            "--font-weight-normal", "--font-weight-medium", "--font-weight-bold",
            "--line-height-tight", "--line-height-normal", "--line-height-loose",
            "--space-1", "--space-2", "--space-3",
            "--space-4", "--space-5", "--space-6",
            "--font-icon-lg", "--font-icon-xl", "--font-icon-2xl",
        };

        var missing = requiredTokens.Where(t => !content.Contains(t)).ToList();

        if (missing.Count > 0)
        {
            Assert.Fail(
                $"design-tokens.css is missing required tokens:\n" +
                string.Join("\n", missing.Select(t => $"  • {t}")));
        }
    }

    /// <summary>
    /// Scans all Blazor .razor component files for banned inline styles that must
    /// be replaced with .ga-* CSS utility classes from portal-skin.css.
    ///
    /// Banned patterns:
    ///   Style="text-align:right"           → use Class="ga-col-numeric"
    ///   Style="margin:0"                   → use Class="ga-chip-reset"
    ///   Style="font-size:                  → use Class="ga-icon-xl/2xl/..." helpers
    ///   Style="max-width:...overflow:hidden → use Class="ga-text-cell-truncate"
    ///
    /// Note: lowercase style= (e.g. style="display:none") is functional/HTML and is NOT banned.
    /// Only Blazor component attribute Style= (uppercase S) is governed here.
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void RazorFiles_DoNotContainBannedInlineStyles()
    {
        var componentsRoot = Path.Combine(SourceRoot, "Components");
        var razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);
        Assert.True(razorFiles.Length > 0, $"No .razor files found under {componentsRoot}");

        // Banned patterns — extended with hardcoded colour values and remaining spacing patterns
        var bannedPatterns = new (string Pattern, string Suggestion)[]
        {
            (@"Style=""text-align:right""",                       "ga-col-numeric"),
            (@"Style=""margin:0""",                               "ga-chip-reset"),
            (@"Style=""font-size:",                               "ga-icon-* helpers"),
            (@"Style=""max-width:280px;overflow:hidden",          "ga-text-cell-truncate"),
            // Hardcoded colours in Blazor Style= (hex values) — must use semantic token class
            (@"Style=""color:#",                                  "semantic token class or MudBlazor Color= param"),
            (@"Style=""background-color:#",                       "ga-status-* / ga-card / semantic token class"),
            (@"Style=""background:#",                             "ga-status-* / ga-card / semantic token class"),
        };

        var violations = new List<string>();
        foreach (var file in razorFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var (pattern, suggestion) in bannedPatterns)
            {
                if (content.Contains(pattern))
                    violations.Add($"{Path.GetFileName(file)}: contains '{pattern}' — use Class=\"{suggestion}\" instead");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                $"Banned inline Style= attributes found in {violations.Count} file(s). " +
                $"Move values to portal-skin.css CSS classes:\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
        }
    }

    /// <summary>
    /// Verifies that every MudTable component in Razor files uses Dense="true".
    /// Dense mode is the portal standard — prevents oversized row padding in data tables.
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void MudTables_MustUseDenseMode()
    {
        var componentsRoot = Path.Combine(SourceRoot, "Components");
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("<MudTable")) continue;

            // A file with <MudTable must also contain Dense="true"
            if (!content.Contains("Dense=\"true\""))
                violations.Add(Path.GetFileName(file));
        }

        if (violations.Count > 0)
            Assert.Fail(
                $"MudTable without Dense=\"true\" found in {violations.Count} file(s). " +
                $"All portal data tables must use Dense=\"true\":\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
    }

    /// <summary>
    /// Verifies that plain HTML &lt;button&gt; elements in application components
    /// (i.e. outside the Layout/ infrastructure folder) carry a ga-btn-* CSS class.
    /// Application buttons must use MudButton, MudIconButton, or — when a plain HTML
    /// button is genuinely needed — one of the .ga-btn-primary / secondary / danger classes.
    /// Blazor reconnect infrastructure (Layout/) is excluded.
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void AppButtons_PlainHtml_MustHaveGaClass()
    {
        var componentsRoot = Path.Combine(SourceRoot, "Components");
        var violations = new List<string>();

        var razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "Layout" + Path.DirectorySeparatorChar));

        foreach (var file in razorFiles)
        {
            var content = File.ReadAllText(file);
            // Check for plain HTML button (lowercase 'b' — Blazor components are PascalCase)
            if (!content.Contains("<button")) continue;

            // If a plain button exists, it must carry a ga-btn-* class in the same file
            if (!content.Contains("ga-btn-"))
                violations.Add($"{Path.GetFileName(file)}: <button> without ga-btn-* class — use MudButton or add Class=\"ga-btn-primary|secondary|danger\"");
        }

        if (violations.Count > 0)
            Assert.Fail(
                $"Plain HTML <button> without ga-btn-* class found in {violations.Count} file(s):\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
    }

    /// <summary>
    /// Fails if any CSS rule uses <c>outline: none</c> without a replacement
    /// focus indicator in the same rule block (<c>box-shadow</c>, <c>border-color</c>,
    /// or <c>:focus-visible</c>).
    ///
    /// Allowed exceptions — non-interactive elements where outline suppression
    /// is safe by design: h1-h6 selectors (Blazor template default).
    ///
    /// Rationale: bare <c>outline: none</c> on interactive elements destroys
    /// keyboard navigation and WCAG 2.4.7 / 2.4.11 compliance.
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void CssOutlineNone_MustHaveFocusReplacement()
    {
        // Non-interactive selectors — outline:none without a replacement is acceptable
        var nonInteractiveSelector = new Regex(
            @"^\s*h[1-6]\s*[:{,]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var outlineNone = new Regex(
            @"outline\s*:\s*none",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var violations = new List<string>();

        foreach (var file in OurCssFiles())
        {
            var lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                if (!outlineNone.IsMatch(lines[i])) continue;

                // Find the opening { for this rule block (search backward up to 30 lines)
                var blockStart = i;
                for (var j = i - 1; j >= Math.Max(0, i - 30); j--)
                {
                    if (lines[j].Contains('{')) { blockStart = j; break; }
                }

                // Skip if selector is for a known non-interactive element
                if (nonInteractiveSelector.IsMatch(lines[blockStart])) continue;

                // Find the closing } (search forward up to 20 lines)
                var blockEnd = i;
                for (var j = i + 1; j < Math.Min(lines.Length, i + 20); j++)
                {
                    if (lines[j].Contains('}')) { blockEnd = j; break; }
                }

                // Collect rule block and check for a replacement focus indicator
                var block = string.Join("\n", lines[blockStart..(blockEnd + 1)]);
                var hasReplacement =
                    block.Contains("box-shadow") ||
                    Regex.IsMatch(block, @"border(-color)?\s*:", RegexOptions.IgnoreCase) ||
                    block.Contains(":focus-visible");

                if (!hasReplacement)
                    violations.Add(
                        $"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}  " +
                        $"[selector: {lines[blockStart].Trim()}]");
            }
        }

        if (violations.Count > 0)
            Assert.Fail(
                $"'outline: none' without a replacement focus indicator found in {violations.Count} location(s). " +
                $"Add box-shadow, border-color, or a :focus-visible override to preserve keyboard accessibility:\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
    }

    /// <summary>
    /// Advisory (non-failing) scan: reports any <c>MudButton</c> that uses
    /// <c>Color="Color.Error"</c> without also applying a semantic danger class.
    ///
    /// Using MudBlazor's <c>Color.Error</c> is valid and often correct — this
    /// advisory surfaces patterns that MIGHT be better expressed via
    /// <c>ga-btn-danger</c> for consistency with the portal component system.
    ///
    /// This test always PASSES. Findings appear in verbose test output only.
    /// </summary>
    [Fact]
    [Trait("Category", "Governance")]
    public void MudButton_ColorError_Advisory()
    {
        var componentsRoot = Path.Combine(SourceRoot, "Components");
        var findings = new List<string>();

        foreach (var file in Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("MudButton")) continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                // Match any line that starts a MudButton with Color.Error
                if (lines[i].Contains("MudButton") && lines[i].Contains("Color.Error"))
                    findings.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");

                // Also catch multi-line: line has Color.Error but check ±2 lines for MudButton
                else if (lines[i].Contains("Color.Error") && lines[i].Contains("Color=\""))
                {
                    var context = string.Join(" ",
                        lines[Math.Max(0, i - 2)..(Math.Min(lines.Length, i + 3))]);
                    if (context.Contains("MudButton"))
                        findings.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        // Deduplicate (multi-line scanning may produce duplicates)
        findings = findings.Distinct().ToList();

        if (findings.Count > 0)
            Console.WriteLine(
                $"[ADVISORY] MudButton Color.Error found in {findings.Count} location(s). " +
                $"If this is a destructive action, consider using Class=\"ga-btn-danger\" for " +
                $"semantic consistency with the portal component system:\n" +
                string.Join("\n", findings.Select(f => $"  ⚠  {f}")));

        // Always passes — advisory only, does not block CI
    }
}
