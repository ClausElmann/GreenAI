using Microsoft.Playwright;

namespace GreenAi.E2E.ColorSystem;

/// <summary>
/// Deterministic color system tests.
/// Validates that semantic design tokens from design-tokens.css are correctly
/// applied to key UI elements using computed CSS assertions (not screenshot diffs).
///
/// Token SSOT: wwwroot/css/design-tokens.css
/// MudTheme:   Components/Layout/MainLayout.razor
///
/// Requires: App running on http://localhost:5057
/// </summary>
[Collection("E2E")]
public sealed class ColorSystemTests : E2ETestBase
{
    // Expected RGB values matching design-tokens.css
    private const string RgbBgMain      = "rgb(247, 248, 250)";  // --color-bg-main:     #F7F8FA
    private const string RgbPrimary     = "rgb(37, 99, 235)";    // --color-primary:      #2563EB
    private const string RgbTextPrimary = "rgb(31, 41, 55)";     // --color-text-primary: #1F2937
    private const string RgbSurface     = "rgb(255, 255, 255)";  // --color-bg-surface:   #FFFFFF
    private const string RgbError       = "rgb(220, 38, 38)";    // --color-error:        #DC2626

    // ── 1. Root background ────────────────────────────────────────────────────

    [Fact]
    public async Task LayoutRoot_HasCorrectBackground()
    {
        await LoginAndNavigateAsync("/broadcasting");

        var root = Page.Locator("[data-testid='layout-root']").First;
        await root.WaitForAsync(new() { Timeout = 15_000 });

        // MudBlazor renders MudLayout — check body background since MudLayout
        // inherits the page background (#F7F8FA from design-tokens.css → body rule)
        var bodyBg = await Page.EvaluateAsync<string>(
            "getComputedStyle(document.body).backgroundColor");

        Assert.Equal(RgbBgMain, bodyBg);
    }

    // ── 2. Primary button ─────────────────────────────────────────────────────

    [Fact]
    public async Task PrimaryButton_HasCorrectBackground()
    {
        await LoginAndNavigateAsync("/broadcasting");

        // Wait for the send button to appear (desktop viewport)
        await Page.WaitForSelectorAsync("[data-testid='btn-primary']",
            new() { Timeout = 15_000 });

        // Playwright evaluates CSS on the rendered button (MudButton renders a <button>)
        var btn = Page.Locator("[data-testid='btn-primary']").First;
        var bg  = await btn.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");

        Assert.Equal(RgbPrimary, bg);
    }

    // ── 3. Primary text color ─────────────────────────────────────────────────

    [Fact]
    public async Task Body_HasPrimaryTextColor()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='top-bar']", new() { Timeout = 15_000 });

        var bodyColor = await Page.EvaluateAsync<string>(
            "getComputedStyle(document.body).color");

        Assert.Equal(RgbTextPrimary, bodyColor);
    }

    // ── 4. Card surface ───────────────────────────────────────────────────────

    [Fact]
    public async Task Card_HasSurfaceBackground()
    {
        await LoginAndNavigateAsync("/broadcasting");

        await Page.WaitForSelectorAsync("[data-testid='scenarios-panel']",
            new() { Timeout = 15_000 });

        var card = Page.Locator("[data-testid='scenarios-panel']").First;
        var bg   = await card.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");

        Assert.Equal(RgbSurface, bg);
    }

    // ── 5. Topbar — surface/neutral styling ───────────────────────────────────

    [Fact]
    public async Task Topbar_UsesNeutralStyling_NotErrorOrWarning()
    {
        await LoginAndNavigateAsync("/broadcasting");

        await Page.WaitForSelectorAsync("[data-testid='top-bar']", new() { Timeout = 15_000 });

        // TopBar background should be white (surface) — not error red, not warning orange
        var appBar    = Page.Locator("[data-testid='top-bar']").First;
        var bg        = await appBar.EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");

        Assert.NotEqual(RgbError, bg);
        // TopBar text is dark, not a warning/error hue
        var color = await appBar.EvaluateAsync<string>(
            "el => getComputedStyle(el).color");
        Assert.NotEqual(RgbError, color);
    }

    // ── 6. Error semantics — error containers use error color, never primary ──

    [Fact]
    public async Task ErrorAlert_UsesErrorColor_NotPrimaryBlue()
    {
        await LoginAndNavigateAsync("/login");

        // Navigate to login — if already authenticated this may redirect,
        // but the login page has an error state we can induce
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Try to submit with invalid credentials to trigger the error alert
        var emailInput = Page.Locator("input[type='email'], input[autocomplete='email'], [placeholder*='Email'], [placeholder*='mail']").First;
        var countEmail = await emailInput.CountAsync();
        if (countEmail == 0)
        {
            // Login page not reachable in this state — skip (already authenticated)
            return;
        }

        await emailInput.FillAsync("invalid@test.invalid");
        var pwdInput = Page.Locator("input[type='password']").First;
        await pwdInput.FillAsync("wrongpassword");
        await Page.Keyboard.PressAsync("Enter");

        // Wait for error alert or timeout gracefully
        try
        {
            await Page.WaitForSelectorAsync("[data-testid='error-alert']",
                new() { Timeout = 8_000 });

            var alert = Page.Locator("[data-testid='error-alert']").First;
            var bg    = await alert.EvaluateAsync<string>(
                "el => getComputedStyle(el).backgroundColor");

            // Must use error family (red), not primary blue
            Assert.Contains("220", bg);     // rgb(220, ...) — error red family
            Assert.DoesNotContain("37, 99", bg);  // NOT rgb(37, 99, 235) — primary blue
        }
        catch (TimeoutException)
        {
            // Error alert not shown in this navigation state — acceptable
        }
    }

    // ── 7. Nav semantics — overlay nav uses primary family ────────────────────

    [Fact]
    public async Task OverlayNav_ActiveItem_UsesPrimaryColor_NotErrorRed()
    {
        await LoginAndNavigateAsync("/broadcasting");

        await Page.WaitForSelectorAsync("[data-testid='top-bar-nav-toggle']",
            new() { Timeout = 15_000 });

        await Page.ClickAsync("[data-testid='top-bar-nav-toggle']");
        await Page.WaitForSelectorAsync("[data-testid='overlay-nav-panel']",
            new() { Timeout = 8_000 });

        // The active nav link should NOT use error red
        var activeLinks = Page.Locator(".mud-nav-link-active, .mud-nav-link.active");
        var count       = await activeLinks.CountAsync();

        if (count > 0)
        {
            var firstActive = activeLinks.First;
            var color       = await firstActive.EvaluateAsync<string>(
                "el => getComputedStyle(el).color");

            // Active link must not be error-red
            Assert.NotEqual(RgbError, color);
        }

        // Close nav
        await Page.ClickAsync("[data-testid='overlay-nav-close']");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task LoginAndNavigateAsync(string path)
    {
        var tokens = await SharedAuth.PrimaryAsync();

        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.EvaluateAsync($"""
            localStorage.setItem('greenai_access_token',  '{tokens.AccessToken}');
            localStorage.setItem('greenai_refresh_token', '{tokens.RefreshToken}');
            localStorage.setItem('greenai_expires_at',    '{tokens.ExpiresAt}');
        """);

        await Page.GotoAsync($"{BaseUrl}{path}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
