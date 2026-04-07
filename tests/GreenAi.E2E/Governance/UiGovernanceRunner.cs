using System.Diagnostics;
using GreenAi.E2E.Assertions;
using Microsoft.Playwright;

namespace GreenAi.E2E.Governance;

/// <summary>
/// Wraps the three Slice-1 governance rules in a try/catch envelope so every rule
/// always returns a <see cref="GovernanceRuleResult"/> — the runner NEVER throws.
///
/// Rule implementations:
///   R1 — same JS threshold as VisualTestBase.AssertNoHorizontalOverflowAsync (tolerance 2px)
///   R2 — delegates to DesignSystemAssertions.AssertTokensDefinedAsync (public static)
///   R3 — same JS logic as VisualTestBase.AssertNoTextOverflowAsync (first 200 elements)
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
