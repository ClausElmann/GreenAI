using Microsoft.Playwright;

namespace GreenAi.E2E.Assertions;

/// <summary>
/// Design-system integrity assertions.
/// Verifies that the rendered page uses tokens and values from the GreenAI design spec.
///
/// These are structural checks — they do not test pixel-accuracy, only that
/// CSS custom properties are defined, key typographic values are in range, and
/// interactive controls meet minimum size requirements.
///
/// Reference spec: docs/SSOT/ui/greenai-ui-skin.md
/// </summary>
public static class DesignSystemAssertions
{
    // ── Allowed typographic scale (px) ────────────────────────────────────────
    // Corresponds to --ga-font-xs (12) through --ga-font-2xl (28).
    // MudBlazor internal elements are excluded from the check.
    private static readonly double[] AllowedFontSizesPx = [11, 12, 13, 14, 15, 16, 18, 20, 24, 28, 32, 36, 48];

    // ── Expected CSS custom property values ──────────────────────────────────
    // Only brand colours — omit palette derivations that could legitimately vary.
    private static readonly IReadOnlyDictionary<string, string> ExpectedTokenValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "--ga-primary",      "#0B5FFF" },
            { "--ga-success",      "#117A37" },
            { "--ga-warning",      "#A15C00" },
            { "--ga-danger",       "#B42318" },
            { "--ga-info",         "#005E7A" },
            { "--ga-focus",        "#111827" },
            { "--ga-text",         "#16202A" },
            { "--ga-text-muted",   "#4B5B6B" },
            { "--ga-surface",      "#FFFFFF" },
            { "--ga-bg",           "#F7F9FC" },
            { "--ga-border",       "#D7DEE7" },
        };

    // ── Allowed border-radius values (px) ────────────────────────────────────
    private static readonly double[] AllowedRadii = [0, 4, 6, 8, 10, 12, 50, 9999];

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that all --ga-* CSS custom properties are defined and have correct values.
    /// Fails if any token is missing or resolves to an unexpected value.
    /// Fast: single JS evaluation.
    /// </summary>
    public static async Task AssertTokensDefinedAsync(IPage page, string? hint = null)
    {
        var tokenJson = System.Text.Json.JsonSerializer.Serialize(ExpectedTokenValues);

        var violations = await page.EvaluateAsync<string[]>($$"""
            () => {
                const expected = {{tokenJson}};
                const cs = getComputedStyle(document.documentElement);
                const out = [];
                for (const [token, expectedVal] of Object.entries(expected)) {
                    const actual = cs.getPropertyValue(token).trim();
                    if (!actual) {
                        out.push(`MISSING: ${token} (expected ${expectedVal})`);
                    } else if (actual.toUpperCase() !== expectedVal.toUpperCase()) {
                        out.push(`MISMATCH: ${token} = "${actual}" (expected "${expectedVal}")`);
                    }
                }
                return out;
            }
            """);

        if (violations is { Length: > 0 })
        {
            var ctx = hint ?? page.Url;
            throw new Exception(
                $"Design token violations on '{ctx}' ({violations.Length}):\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
        }
    }

    /// <summary>
    /// Checks that no visible interactive element (button, link) displays text
    /// smaller than 12px — the design system minimum.
    /// Skips MudBlazor internal elements (.mud-*).
    /// </summary>
    public static async Task AssertFontScaleAsync(IPage page, string? hint = null)
    {
        var tooSmall = await page.EvaluateAsync<string[]>("""
            () => {
                const MIN_PX = 12;
                const out    = [];
                for (const el of document.querySelectorAll('button, a[href], [role="button"], label')) {
                    const cls = typeof el.className === 'string' ? el.className : '';
                    if (cls.includes('mud-')) continue; // MudBlazor internals
                    const r = el.getBoundingClientRect();
                    if (r.width === 0 || r.height === 0) continue;
                    const size = parseFloat(window.getComputedStyle(el).fontSize);
                    if (size < MIN_PX) {
                        const id = el.getAttribute('data-testid')
                                 ?? el.textContent?.trim().slice(0, 30)
                                 ?? el.tagName;
                        out.push(`"${id}" font-size=${size}px`);
                    }
                }
                return [...new Set(out)];
            }
            """);

        if (tooSmall is { Length: > 0 })
        {
            var ctx = hint ?? page.Url;
            throw new Exception(
                $"Font scale violation on '{ctx}' — {tooSmall.Length} interactive element(s) below 12px minimum:\n" +
                string.Join("\n", tooSmall.Take(5).Select(v => $"  • {v}")));
        }
    }

    /// <summary>
    /// Checks that visible MudCard / MudPaper / form elements use border-radius
    /// values matching the allowed token set (6px, 8px, 10px).
    /// Allows 0 (no radius) and large values (circle via 9999px / 50%).
    /// </summary>
    public static async Task AssertBorderRadiusAsync(IPage page, string? hint = null)
    {
        var allowedJson = System.Text.Json.JsonSerializer.Serialize(AllowedRadii);

        var violations = await page.EvaluateAsync<string[]>($$"""
            () => {
                const allowed = new Set({{allowedJson}});
                const TOLERANCE = 1; // sub-pixel
                const out = [];
                const sel = '.mud-card, .mud-paper, .mud-button-root, .mud-input-outlined-border';
                for (const el of document.querySelectorAll(sel)) {
                    const r = el.getBoundingClientRect();
                    if (r.width === 0) continue;
                    const cs  = window.getComputedStyle(el);
                    const raw = parseFloat(cs.borderRadius);
                    if (isNaN(raw)) continue;
                    const ok  = [...allowed].some(a => Math.abs(raw - a) <= TOLERANCE);
                    if (!ok) {
                        const id = el.getAttribute('data-testid')
                                 ?? el.className?.split(' ').slice(0,3).join('.')
                                 ?? el.tagName;
                        out.push(`"${id}" border-radius=${raw}px (not in allowed set)`);
                    }
                }
                return [...new Set(out)].slice(0, 10);
            }
            """);

        if (violations is { Length: > 0 })
        {
            var ctx = hint ?? page.Url;
            throw new Exception(
                $"Border-radius violation on '{ctx}' ({violations.Length}):\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
        }
    }

    /// <summary>
    /// Checks that visible block elements (cards, panels, main areas) use
    /// padding and margin values from the --ga-space-* scale: 4/8/12/16/24/32/40px.
    /// Allows 0 and values >=44px (touch targets, large layout gutters).
    /// Skips MudBlazor internals and invisible elements.
    /// </summary>
    public static async Task AssertSpacingScaleAsync(IPage page, string? hint = null)
    {
        // Allowed spacing values (px) matching --ga-space-1 through --ga-space-7
        var allowed = new double[] { 0, 2, 4, 8, 12, 16, 24, 32, 40, 44, 48, 56, 64, 72 };
        var allowedJson = System.Text.Json.JsonSerializer.Serialize(allowed);

        var violations = await page.EvaluateAsync<string[]>($$"""
            () => {
                const allowed = new Set({{allowedJson}});
                const TOLERANCE = 0.5;
                const SELECTOR  = '.mud-card, .hub-panel, .ga-page-content, .mud-dialog-content, .mud-dialog-actions';
                const PROPS     = ['paddingTop','paddingBottom','paddingLeft','paddingRight','marginTop','marginBottom'];
                const out       = [];

                for (const el of document.querySelectorAll(SELECTOR)) {
                    const r = el.getBoundingClientRect();
                    if (r.width === 0 || r.height === 0) continue;
                    const cs = window.getComputedStyle(el);
                    for (const prop of PROPS) {
                        const val = parseFloat(cs[prop]);
                        if (isNaN(val)) continue;
                        const ok = [...allowed].some(a => Math.abs(val - a) <= TOLERANCE);
                        if (!ok) {
                            const id = el.getAttribute('data-testid')
                                     ?? el.className?.split(' ').slice(0,3).join('.')
                                     ?? el.tagName;
                            out.push(`"${id}" ${prop}=${val}px`);
                        }
                    }
                }
                return [...new Set(out)].slice(0, 8);
            }
            """);

        if (violations is { Length: > 0 })
        {
            var ctx = hint ?? page.Url;
            throw new Exception(
                $"Spacing scale violation on '{ctx}' ({violations.Length} off-scale value(s)):\n" +
                string.Join("\n", violations.Select(v => $"  • {v}")));
        }
    }
}
