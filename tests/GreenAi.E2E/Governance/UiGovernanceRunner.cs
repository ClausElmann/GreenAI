using System.Diagnostics;
using GreenAi.E2E.Assertions;
using Microsoft.Playwright;

namespace GreenAi.E2E.Governance;

/// <summary>
/// Wraps governance rules in a try/catch envelope so every rule
/// always returns a <see cref="GovernanceRuleResult"/> — the runner NEVER throws.
///
/// Rules that delegate to public static methods in DesignSystemAssertions:
///   R2 — AssertTokensDefinedAsync
///   R6 — AssertSpacingScaleAsync
///   R7 — AssertFontScaleAsync
///
/// Rules implemented inline (protected in VisualTestBase — same JS/thresholds, no structural change):
///   R1 — AssertNoHorizontalOverflowAsync
///   R3 — AssertNoTextOverflowAsync
///   R4 — AssertTopBarNotClippingContentAsync
///   R5 — AssertNoOverlappingClickableElementsAsync
///
/// Neither VisualTestBase nor any existing assertion file is modified.
/// </summary>
public sealed class UiGovernanceRunner
{
    private readonly IPage _page;

    public UiGovernanceRunner(IPage page)
    {
        _page = page;
    }

    public async Task<List<GovernanceRuleResult>> RunAsync()
    {
        return
        [
            await ExecuteAsync(
                ruleKey:   "layout.no_horizontal_overflow",
                ruleId:    "AssertNoHorizontalOverflowAsync",
                ruleName:  "NoHorizontalOverflow",
                severity:  "major",
                action:    AssertNoHorizontalOverflowAsync),

            await ExecuteAsync(
                ruleKey:   "tokens.primary_color",
                ruleId:    "AssertTokensDefinedAsync",
                ruleName:  "ColorTokenPrimary",
                severity:  "critical",
                action:    () => DesignSystemAssertions.AssertTokensDefinedAsync(_page)),

            await ExecuteAsync(
                ruleKey:   "typography.no_text_overflow",
                ruleId:    "AssertNoTextOverflowAsync",
                ruleName:  "NoTextOverflow",
                severity:  "minor",
                action:    AssertNoTextOverflowAsync),

            // ── Slice 2 ───────────────────────────────────────────────────────

            await ExecuteAsync(
                ruleKey:   "layout.topbar_not_clipping",
                ruleId:    "AssertTopBarNotClippingContentAsync",
                ruleName:  "TopBarNotClipping",
                severity:  "major",
                action:    AssertTopBarNotClippingAsync),

            await ExecuteAsync(
                ruleKey:   "z-index.no_overlapping_clickable",
                ruleId:    "AssertNoOverlappingClickableElementsAsync",
                ruleName:  "NoOverlappingClickableElements",
                severity:  "major",
                action:    AssertNoOverlappingClickableElementsAsync),

            await ExecuteAsync(
                ruleKey:   "layout.spacing_scale",
                ruleId:    "AssertSpacingScaleAsync",
                ruleName:  "SpacingScale",
                severity:  "minor",
                action:    () => DesignSystemAssertions.AssertSpacingScaleAsync(_page)),

            await ExecuteAsync(
                ruleKey:   "typography.font_scale",
                ruleId:    "AssertFontScaleAsync",
                ruleName:  "FontScale",
                severity:  "minor",
                action:    () => DesignSystemAssertions.AssertFontScaleAsync(_page)),
        ];
    }

    // ── Rule implementations ──────────────────────────────────────────────────
    // Same DOM conditions and thresholds as VisualTestBase — no changes to existing files.

    /// <summary>
    /// R1: Horizontal overflow — tolerance 2px.
    /// Mirrors VisualTestBase.AssertNoHorizontalOverflowAsync.
    /// </summary>
    private async Task AssertNoHorizontalOverflowAsync()
    {
        var overflow = await _page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - window.innerWidth)");

        if (overflow > 2)
            throw new Exception(
                $"Horizontal overflow: scrollWidth exceeds innerWidth by {overflow}px at {_page.Url}. " +
                "Check for elements wider than viewport or missing max-width/overflow-x:hidden.");
    }

    /// <summary>
    /// R3: Text overflow — checks first 200 text-bearing elements.
    /// Mirrors VisualTestBase.AssertNoTextOverflowAsync.
    /// </summary>
    private async Task AssertNoTextOverflowAsync()
    {
        var clipped = await _page.EvaluateAsync<string[]>("""
            () => {
                const out = [];
                let n = 0;
                const tags = ['p','span','h1','h2','h3','h4','h5','h6','li','td','th',
                              'label','button','a','div'];
                for (const tag of tags) {
                    for (const el of document.querySelectorAll(tag)) {
                        if (++n > 200) break;
                        if (!el.textContent?.trim()) continue;
                        const r = el.getBoundingClientRect();
                        if (r.width === 0 || r.height === 0) continue;
                        const cs = window.getComputedStyle(el);
                        const hasClip     = cs.overflow === 'hidden' || cs.overflowX === 'hidden';
                        const hasEllipsis = cs.textOverflow === 'ellipsis';
                        if (hasClip && hasEllipsis && el.scrollWidth > el.clientWidth + 4) {
                            const id = el.getAttribute('data-testid')
                                     ?? el.textContent.trim().slice(0, 30)
                                     ?? el.tagName;
                            out.push(`"${id}" clipped: scrollW=${el.scrollWidth} clientW=${el.clientWidth}`);
                        }
                    }
                }
                return [...new Set(out)];
            }
            """);

        if (clipped.Length > 0)
            throw new Exception(
                $"{clipped.Length} text element(s) have overflowing/clipped text:\n" +
                string.Join("\n", clipped.Take(5)) +
                (clipped.Length > 5 ? $"\n…and {clipped.Length - 5} more." : ""));
    }

    // ── Slice 2 inline implementations ───────────────────────────────────────

    /// <summary>
    /// R4: TopBar not clipping content — tolerance 4px.
    /// Mirrors VisualTestBase.AssertTopBarNotClippingContentAsync.
    /// </summary>
    private async Task AssertTopBarNotClippingAsync()
    {
        var topBarBottom = await _page.EvaluateAsync<double>("""
            () => {
                const el = document.querySelector('[data-testid="top-bar"]');
                return el ? el.getBoundingClientRect().bottom : -1;
            }
            """);

        if (topBarBottom < 0) return; // top-bar absent — skip

        var paddingTop = await _page.EvaluateAsync<double>("""
            () => {
                const el = document.querySelector('.mud-main-content');
                if (!el) return -1;
                return parseFloat(getComputedStyle(el).paddingTop) || 0;
            }
            """);

        if (paddingTop < 0) return; // mud-main-content absent — skip

        if (paddingTop < topBarBottom - 4)
            throw new Exception(
                $"MudMainContent paddingTop ({paddingTop:F0}px) is less than TopBar bottom ({topBarBottom:F0}px) — " +
                "main content is being clipped by the app bar.");
    }

    /// <summary>
    /// R5: No overlapping clickable elements — centre-point elementFromPoint check.
    /// Mirrors VisualTestBase.AssertNoOverlappingClickableElementsAsync.
    /// </summary>
    private async Task AssertNoOverlappingClickableElementsAsync()
    {
        var blocked = await _page.EvaluateAsync<string[]>("""
            () => {
                const vw  = window.innerWidth;
                const vh  = window.innerHeight;
                const sel = 'button:not([disabled]), a[href]:not([tabindex="-1"]), [role="button"]:not([disabled])';
                const out = [];
                for (const el of document.querySelectorAll(sel)) {
                    const r = el.getBoundingClientRect();
                    if (r.width === 0 || r.height === 0) continue;
                    const cx = r.left + r.width  / 2;
                    const cy = r.top  + r.height / 2;
                    if (cx < 0 || cy < 0 || cx > vw || cy > vh) continue;
                    const top = document.elementFromPoint(cx, cy);
                    if (!top || el.contains(top) || top.contains(el)) continue;
                    const blockerId = top.getAttribute('data-testid')
                                   ?? top.className?.split(' ').slice(0, 3).join('.')
                                   ?? top.tagName;
                    const targetId  = el.getAttribute('data-testid')
                                   ?? el.textContent?.trim().slice(0, 30)
                                   ?? el.tagName;
                    out.push(`"${targetId}" blocked by "${blockerId}"`);
                }
                return [...new Set(out)];
            }
            """);

        if (blocked.Length > 0)
            throw new Exception(
                $"{blocked.Length} interactive element(s) are obscured by overlapping elements:\n" +
                string.Join("\n", blocked.Take(5)) +
                (blocked.Length > 5 ? $"\n…and {blocked.Length - 5} more." : ""));
    }

    // ── Execution wrapper ─────────────────────────────────────────────────────

    private static async Task<GovernanceRuleResult> ExecuteAsync(
        string ruleKey,
        string ruleId,
        string ruleName,
        string severity,
        Func<Task> action)
    {
        var sw = Stopwatch.StartNew();

        var result = new GovernanceRuleResult
        {
            RuleKey  = ruleKey,
            RuleId   = ruleId,
            RuleName = ruleName,
            Severity = severity,
        };

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            result.Passed  = false;
            result.Message = ex.Message;
        }
        finally
        {
            sw.Stop();
            result.ExecutionMs = (int)sw.ElapsedMilliseconds;
        }

        return result;
    }
}
