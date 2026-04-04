using Microsoft.Playwright;

namespace GreenAi.E2E.Visual;

/// <summary>
/// Multi-device visual tests for the UI shell navigation.
/// Each test runs against all 4 device profiles (Desktop, Laptop, Tablet, Mobile).
///
/// What is validated per device:
///   - Screenshot is captured (current + baseline on first run)
///   - No horizontal overflow
///   - TopBar does not clip main content
///
/// What is NOT validated (pixel regression):
///   - Pixel-diff against baseline is deferred until UI is stable.
///   - Baseline images live in TestResults/Visual/baseline/ — ready for diff implementation.
///   - To update baselines after intentional UI change:
///       $env:GREENAI_UPDATE_BASELINE="true"
///       dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" --nologo
///       $env:GREENAI_UPDATE_BASELINE=$null
///
/// Requires:
///   - App running on http://localhost:5057
///   - admin@dev.local seeded (E2EDatabaseFixture)
/// </summary>
public sealed class NavigationVisualTests : VisualTestBase
{
    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Fact]
    public Task Dashboard_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='top-bar']",               new() { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='dashboard-placeholder']", new() { Timeout = 10_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertInteractiveElementsVisibleAsync(page, device);
            await AssertNoOverlappingClickableElementsAsync(page, device);
            await AssertLayoutConsistencyAsync(page, device);
            await AssertReasonableSpacingAsync(page, device);
            await AssertNavigationUsableAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "dashboard");
        });

    // ── OverlayNav ────────────────────────────────────────────────────────────

    [Fact]
    public Task OverlayNav_Open_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='top-bar-nav-toggle']", new() { Timeout = 15_000 });

            // Trigger overlay nav
            await page.ClickAsync("[data-testid='top-bar-nav-toggle']");
            await page.WaitForSelectorAsync("[data-testid='overlay-nav-panel']", new() { Timeout = 8_000 });

            // Panel is open — assert no overflow and capture
            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNavigationUsableAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "overlay-nav-open");

            // Close via the dedicated close button — more reliable on mobile than backdrop click
            // (on narrow viewports the panel can cover the entire backdrop area)
            await page.ClickAsync("[data-testid='overlay-nav-close']");
            await page.WaitForSelectorAsync("[data-testid='overlay-nav-panel']",
                new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
        });

    [Fact]
    public Task OverlayNav_ESCCloses_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='top-bar-nav-toggle']", new() { Timeout = 15_000 });

            await page.ClickAsync("[data-testid='top-bar-nav-toggle']");
            await page.WaitForSelectorAsync("[data-testid='overlay-nav-panel']", new() { Timeout = 8_000 });

            // ESC should close
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForSelectorAsync("[data-testid='overlay-nav-panel']",
                new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
        });

    // ── CommandPalette ────────────────────────────────────────────────────────

    [Fact]
    public Task CommandPalette_Open_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='top-bar']",           new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Ctrl+K triggers the palette via JS keyboard-shortcuts.js
            await page.Keyboard.PressAsync("Control+k");
            await page.WaitForSelectorAsync("[data-testid='cmd-palette-panel']", new() { Timeout = 8_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "command-palette-open");

            // ESC should close
            await page.Keyboard.PressAsync("Escape");
        });

    [Fact]
    public Task CommandPalette_TypeFilters_AndNavigates_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Keyboard.PressAsync("Control+k");
            await page.WaitForSelectorAsync("[data-testid='cmd-palette-input']", new() { Timeout = 8_000 });

            // Type a query — results should filter
            await page.FillAsync("[data-testid='cmd-palette-input']", "profil");

            // At least one result must remain visible
            var count = await page.Locator("[data-testid='cmd-item']").CountAsync();
            if (count == 0)
                throw new Exception($"[{device.Name}] Typing 'profil' returned 0 results — filtering may be broken.");

            await CaptureAsync(page, device, "command-palette-filtered");

            // Press Enter on the active item — palette closes and navigates to the matched route.
            // Min profil / user profile should be the first match for "profil".
            await page.Keyboard.PressAsync("Enter");

            // Palette must close
            await page.WaitForSelectorAsync("[data-testid='cmd-palette-panel']",
                new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });

            // URL must have changed to the selected route
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < deadline && page.Url == $"{BaseUrl}/dashboard")
                await Task.Delay(100, TestContext.Current.CancellationToken);

            if (!page.Url.Contains("/user/profile") && !page.Url.Contains("/customer"))
                throw new Exception(
                    $"[{device.Name}] CommandPalette Enter did not navigate away from dashboard. "
                    + $"Current URL: {page.Url}");
        });

    // ── Viewport resize ──────────────────────────────────────────────────────

    /// <summary>
    /// Starts at desktop width, then resizes mid-session to mobile width.
    /// Catches responsive bugs that only appear on resize (not initial load):
    ///   - elements that fail to reflow
    ///   - sticky overlays that become clipped
    ///   - overflow that only appears after reflow
    /// </summary>
    [Fact]
    public async Task Resize_Desktop_To_Mobile()
    {
        // Single-context test — the resize IS the multi-device scenario.
        using var playwright = await Playwright.CreateAsync();
        // Match VisualTestBase: Headless=false for local runs — headless Chromium has
        // WebSocket issues that prevent the Blazor circuit from establishing.
        var isHeadless = Environment.GetEnvironmentVariable("GREENAI_VISUAL_HEADLESS") is "true";
        var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = isHeadless,
            SlowMo   = isHeadless ? 0 : 80,
        });
        var ctx     = await browser.NewContextAsync(new()
        {
            ViewportSize      = new() { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
        });
        var page = await ctx.NewPageAsync();

        try
        {
            // 1. Login and verify at desktop
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='top-bar']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var desktopProfile = new DeviceProfile("Desktop", 1920, 1080);
            await AssertNoHorizontalOverflowAsync(page, desktopProfile);
            await AssertTopBarNotClippingContentAsync(page, desktopProfile);
            await CaptureAsync(page, desktopProfile, "resize_before");

            // 2. Resize mid-session to mobile dimensions
            await page.SetViewportSizeAsync(390, 844);
            await page.WaitForTimeoutAsync(250); // let CSS reflow settle

            // 3. Assert layout still valid after resize
            var mobileProfile = new DeviceProfile("Mobile", 390, 844, IsMobile: true);
            await AssertNoHorizontalOverflowAsync(page, mobileProfile);
            await AssertTopBarNotClippingContentAsync(page, mobileProfile);
            await CaptureAsync(page, mobileProfile, "resize_after");
        }
        finally
        {
            await page.CloseAsync();
            await ctx.CloseAsync();
            await browser.DisposeAsync();
        }
    }

    // ── UserProfile ───────────────────────────────────────────────────────────

    [Fact]
    public Task UserProfile_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/user/profile");
            await page.WaitForSelectorAsync("[data-testid='display-name-input']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertInteractiveElementsVisibleAsync(page, device);
            await AssertNoOverlappingClickableElementsAsync(page, device);
            await AssertLayoutConsistencyAsync(page, device);
            await AssertReasonableSpacingAsync(page, device);
            await AssertNavigationUsableAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "user-profile");
        });
}
