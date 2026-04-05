using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace GreenAi.E2E.Assertions;

/// <summary>
/// Axe-core WCAG accessibility assertions.
///
/// Usage:
///   await AccessibilityAssertions.AssertNoViolationsAsync(page);
///
/// Controlled by environment variable:
///   GREENAI_ACCESSIBILITY_GATES=true   → runs and fails on violations (default in CI)
///   GREENAI_ACCESSIBILITY_GATES=warn   → runs and logs but never fails
///   (unset / false)                    → skips axe entirely (default for speed)
///
/// Excluded selectors are MudBlazor internals not under our control.
/// </summary>
public static class AccessibilityAssertions
{
    private static readonly string[] ExcludedSelectors =
    [
        // MudBlazor SVG icons lack accessible title — upstream issue
        ".mud-icon-root",
        // MudBlazor pickers use aria-hidden on parts of their ARIA tree
        ".mud-picker",
        ".mud-picker-content",
        // MudBlazor decorative dividers
        ".mud-divider",
        // Blazor SignalR reconnection overlay — not a prod UI element
        "#components-reconnect-modal",
    ];

    /// <summary>
    /// Runs axe-core (WCAG 2.0 A + AA) against the current page state.
    /// Saves a screenshot to TestResults/Accessibility/ on failure.
    /// </summary>
    /// <param name="page">The Playwright page to analyse.</param>
    /// <param name="hint">Optional label for the failure message (e.g. page route).</param>
    /// <exception cref="Exception">Thrown when any WCAG violations are found.</exception>
    public static async Task AssertNoViolationsAsync(IPage page, string? hint = null)
    {
        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = new List<string> { "wcag2a", "wcag2aa" } },
            ResultTypes = new HashSet<ResultType> { ResultType.Violations },
        };

        var context = new AxeRunContext
        {
            Exclude = ExcludedSelectors.Select(s => new AxeSelector(s)).ToList(),
        };

        var result = await page.RunAxe(context, options);

        if (result.Violations.Length == 0)
            return;

        // ── Build rich failure message ───────────────────────────────────────
        var lines = new List<string>
        {
            $"WCAG violation(s) found: {result.Violations.Length}",
            hint is not null ? $"Page:    {hint}" : $"URL:     {page.Url}",
            "",
        };

        foreach (var v in result.Violations)
        {
            lines.Add($"[{(v.Impact ?? "unknown").ToUpperInvariant()}] {v.Id}");
            lines.Add($"  Rule:   {v.Description}");
            lines.Add($"  Help:   {v.HelpUrl}");
            foreach (var node in v.Nodes.Take(2))
            {
                var html = (node.Html ?? "").Trim();
                if (html.Length > 120) html = html[..120] + "…";
                lines.Add($"  Target: {html}");
            }
            if (v.Nodes.Length > 2)
                lines.Add($"  … and {v.Nodes.Length - 2} more element(s).");
            lines.Add("");
        }

        // ── Screenshot ───────────────────────────────────────────────────────
        var screenshotPath = await CaptureViolationScreenshotAsync(page);
        if (screenshotPath is not null)
            lines.Add($"Screenshot: {screenshotPath}");

        throw new Exception(string.Join("\n", lines));
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private static async Task<string?> CaptureViolationScreenshotAsync(IPage page)
    {
        try
        {
            var dir = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "TestResults", "Accessibility");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"axe-{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            return path;
        }
        catch
        {
            return null;
        }
    }
}
