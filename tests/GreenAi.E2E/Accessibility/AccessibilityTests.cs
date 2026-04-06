using GreenAi.E2E.Assertions;
using GreenAi.E2E.Visual;
using Microsoft.Playwright;

namespace GreenAi.E2E.Accessibility;

/// <summary>
/// WCAG 2.1 AA accessibility tests — always on, no env var gate.
///
/// Coverage:
///   /broadcasting, /status, /drafts, /user/profile
///
/// Each test runs axe-core (wcag2a + wcag2aa) across all 4 device viewports.
/// Fails on: contrast violations, missing labels, ARIA issues, missing button names.
///
/// Keyboard navigation:
///   - Tab cycles focus with visible ring (outline-width ≥ 2px)
///   - ESC closes overlays: covered by NavigationVisualTests.OverlayNav_ESCCloses_AllDevices
/// </summary>
[Collection("E2E")]
public sealed class AccessibilityTests : VisualTestBase
{
    // ── Axe scans: key pages ─────────────────────────────────────────────────

    [Fact]
    public Task Broadcasting_NoWcagViolations()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            // LoginAsync → /dashboard → redirects to /broadcasting
            await page.WaitForSelectorAsync("[data-testid='send-methods-grid']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await AccessibilityAssertions.AssertNoViolationsAsync(page, $"{device.Name}/broadcasting");
        });

    [Fact]
    public Task Status_NoWcagViolations()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/status");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await AccessibilityAssertions.AssertNoViolationsAsync(page, $"{device.Name}/status");
        });

    [Fact]
    public Task Drafts_NoWcagViolations()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/drafts");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await AccessibilityAssertions.AssertNoViolationsAsync(page, $"{device.Name}/drafts");
        });

    [Fact]
    public Task UserProfile_NoWcagViolations()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/user/profile");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await AccessibilityAssertions.AssertNoViolationsAsync(page, $"{device.Name}/user-profile");
        });

    // ── Keyboard navigation ───────────────────────────────────────────────────

    /// <summary>
    /// Presses Tab 8 times on /broadcasting and verifies each focused element
    /// has a visible focus ring (outline-width ≥ 2px, WCAG 2.4.11).
    /// Invisible or body-focused stops are skipped.
    /// </summary>
    [Fact]
    public Task KeyboardFocus_TabCycles_VisibleRingOnEachElement_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='top-bar']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var violations = new List<string>();

            for (var i = 0; i < 8; i++)
            {
                await page.Keyboard.PressAsync("Tab");

                // [outlineWidth, elemWidth] — elemWidth=0 means hidden/invisible
                var metrics = await page.EvaluateAsync<double[]>("""
                    () => {
                        const el = document.activeElement;
                        if (!el || el === document.body) return [-1, -1];
                        const cs = window.getComputedStyle(el);
                        const r  = el.getBoundingClientRect();
                        return [
                            parseFloat(cs.outlineWidth) || 0,
                            r.width
                        ];
                    }
                    """);

                var outlineWidth = metrics[0];
                var elemWidth    = metrics[1];

                if (outlineWidth < 0 || elemWidth == 0) continue; // body or hidden — skip

                if (outlineWidth < 2)
                {
                    var label = await page.EvaluateAsync<string>("""
                        () => {
                            const el = document.activeElement;
                            if (!el) return "?";
                            return el.getAttribute('data-testid')
                                ?? el.getAttribute('aria-label')
                                ?? el.textContent?.trim().slice(0, 40)
                                ?? el.tagName;
                        }
                        """);
                    violations.Add($"Tab {i + 1}: '{label}' outline-width={outlineWidth}px (need ≥ 2px, WCAG 2.4.11)");
                }
            }

            if (violations.Count > 0)
                throw new Exception(
                    $"[{device.Name}] Focus ring missing or thin on {violations.Count} element(s):\n" +
                    string.Join("\n", violations));
        });
}
