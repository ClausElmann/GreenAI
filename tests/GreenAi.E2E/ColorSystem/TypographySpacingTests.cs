using Microsoft.Playwright;

namespace GreenAi.E2E.ColorSystem;

/// <summary>
/// Deterministic typography + spacing system tests.
/// Validates that semantic tokens from design-tokens.css are correctly applied
/// to body text, headings, and cards using computed CSS assertions.
///
/// Token SSOT: wwwroot/css/design-tokens.css
/// Applied in: wwwroot/css/portal-skin.css
///
/// Requires: App running on http://localhost:5057
/// </summary>
[Collection("E2E")]
public sealed class TypographySpacingTests : E2ETestBase
{
    // ── 1. Body font-size = 16px (--font-md) ─────────────────────────────────

    [Fact]
    public async Task Body_FontSize_Is16px()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='layout-root']", new() { Timeout = 15_000 });

        var fontSize = await Page.EvaluateAsync<string>(
            "getComputedStyle(document.body).fontSize");

        Assert.Equal("16px", fontSize);
    }

    // ── 2. Body line-height = 1.5 (--line-height-normal) ─────────────────────

    [Fact]
    public async Task Body_LineHeight_IsNormal()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='layout-root']", new() { Timeout = 15_000 });

        // Computed line-height is returned as px, resolved against 16px base
        // 16px × 1.5 = 24px
        var lineHeight = await Page.EvaluateAsync<string>(
            "getComputedStyle(document.body).lineHeight");

        Assert.Equal("24px", lineHeight);
    }

    // ── 3. h1 font-size = 24px (--font-2xl) ──────────────────────────────────

    [Fact]
    public async Task H1_FontSize_Is24px()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='layout-root']", new() { Timeout = 15_000 });

        // Check if any h1 exists in the page; if not, assert via CSS custom property resolution
        var h1Count = await Page.Locator("h1").CountAsync();
        if (h1Count > 0)
        {
            var fontSize = await Page.Locator("h1").First.EvaluateAsync<string>(
                "el => getComputedStyle(el).fontSize");
            Assert.Equal("24px", fontSize);
        }
        else
        {
            // Assert token resolves correctly — 24px
            var resolved = await Page.EvaluateAsync<string>(
                "getComputedStyle(document.documentElement).getPropertyValue('--font-2xl').trim()");
            Assert.Equal("24px", resolved);
        }
    }

    // ── 4. h2 font-size = 20px (--font-xl) ───────────────────────────────────

    [Fact]
    public async Task H2_FontSize_Is20px()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='layout-root']", new() { Timeout = 15_000 });

        var h2Count = await Page.Locator("h2").CountAsync();
        if (h2Count > 0)
        {
            var fontSize = await Page.Locator("h2").First.EvaluateAsync<string>(
                "el => getComputedStyle(el).fontSize");
            Assert.Equal("20px", fontSize);
        }
        else
        {
            var resolved = await Page.EvaluateAsync<string>(
                "getComputedStyle(document.documentElement).getPropertyValue('--font-xl').trim()");
            Assert.Equal("20px", resolved);
        }
    }

    // ── 5. Cards have consistent padding (--space-4 = 16px) ──────────────────

    [Fact]
    public async Task Card_Padding_IsSpaceToken()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='scenarios-panel']", new() { Timeout = 15_000 });

        // MudCard content padding should resolve to 16px from --space-4
        var cardContent = Page.Locator(".mud-card-content").First;
        var count = await cardContent.CountAsync();
        if (count > 0)
        {
            var paddingTop = await cardContent.EvaluateAsync<string>(
                "el => getComputedStyle(el).paddingTop");
            // --space-4 = 16px
            Assert.Equal("16px", paddingTop);
        }
        else
        {
            // Assert token is defined correctly
            var resolved = await Page.EvaluateAsync<string>(
                "getComputedStyle(document.documentElement).getPropertyValue('--space-4').trim()");
            Assert.Equal("16px", resolved);
        }
    }

    // ── 6. Spacing tokens are all defined and resolve correctly ───────────────

    [Fact]
    public async Task SpacingTokens_AllResolveCorrectly()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='layout-root']", new() { Timeout = 15_000 });

        var expected = new Dictionary<string, string>
        {
            ["--space-1"] = "4px",
            ["--space-2"] = "8px",
            ["--space-3"] = "12px",
            ["--space-4"] = "16px",
            ["--space-5"] = "24px",
            ["--space-6"] = "32px",
        };

        foreach (var (token, expectedPx) in expected)
        {
            var resolved = await Page.EvaluateAsync<string>(
                $"getComputedStyle(document.documentElement).getPropertyValue('{token}').trim()");
            Assert.True(resolved == expectedPx,
                $"Token {token} expected '{expectedPx}' but resolved to '{resolved}'");
        }
    }

    // ── 7. Typography tokens are all defined and resolve correctly ────────────

    [Fact]
    public async Task TypographyTokens_AllResolveCorrectly()
    {
        await LoginAndNavigateAsync("/broadcasting");
        await Page.WaitForSelectorAsync("[data-testid='layout-root']", new() { Timeout = 15_000 });

        var expected = new Dictionary<string, string>
        {
            ["--font-xs"]  = "12px",
            ["--font-sm"]  = "14px",
            ["--font-md"]  = "16px",
            ["--font-lg"]  = "18px",
            ["--font-xl"]  = "20px",
            ["--font-2xl"] = "24px",
            ["--font-3xl"] = "30px",
        };

        foreach (var (token, expectedPx) in expected)
        {
            var resolved = await Page.EvaluateAsync<string>(
                $"getComputedStyle(document.documentElement).getPropertyValue('{token}').trim()");
            Assert.True(resolved == expectedPx,
                $"Token {token} expected '{expectedPx}' but resolved to '{resolved}'");
        }
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
