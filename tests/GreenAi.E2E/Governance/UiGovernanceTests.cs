using System.Text.Json;
using Microsoft.Playwright;

namespace GreenAi.E2E.Governance;

/// <summary>
/// UI Governance Slice 1 — Browser-based governance tests.
///
/// Runs 3 rules against /broadcasting on a Desktop viewport and writes
/// TestResults/governance-report.json.
///
/// Rules:
///   R1 layout.no_horizontal_overflow  (major)
///   R2 tokens.primary_color           (critical)
///   R3 typography.no_text_overflow    (minor)
///
/// Score weights: critical=50, major=15, minor=5.
/// Fails when: score &lt; 80 OR criticalCount &gt; 0.
///
/// Run:  dotnet test --filter "Category=UIGovernance" --nologo
/// </summary>
[Trait("Category", "UIGovernance")]
public sealed class UiGovernanceTests : IAsyncLifetime
{
    private const string BaseUrl = "http://localhost:5057";

    private IPlaywright _playwright = null!;
    private IBrowser    _browser    = null!;
    private IPage       _page       = null!;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("GREENAI_VISUAL_HEADLESS") is "true",
            SlowMo   = 80,
        });

        var ctx = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize      = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
        });
        _page = await ctx.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BroadcastingPage_Governance()
    {
        await AuthenticateAsync();

        await _page.GotoAsync($"{BaseUrl}/broadcasting");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var runner  = new UiGovernanceRunner(_page);
        var results = await runner.RunAsync();

        var (score, critical, major, minor) = GovernanceScorer.Score(results);

        await WriteReportAsync(score, critical, major, minor, results);

        if (score < 80)
            throw new Exception(
                $"UIGovernance: score too low ({score}/100). " +
                $"See TestResults/governance-report.json for details.");

        if (critical > 0)
            throw new Exception(
                $"UIGovernance: {critical} critical violation(s) found. " +
                $"See TestResults/governance-report.json for details.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task AuthenticateAsync()
    {
        var tokens = await SharedAuth.PrimaryAsync();

        // Establish domain context so localStorage write is accepted
        await _page.GotoAsync($"{BaseUrl}/");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.EvaluateAsync($"""
            localStorage.setItem('greenai_access_token',  '{tokens.AccessToken}');
            localStorage.setItem('greenai_refresh_token', '{tokens.RefreshToken}');
            localStorage.setItem('greenai_expires_at',    '{tokens.ExpiresAt}');
            """);

        await _page.GotoAsync($"{BaseUrl}/dashboard");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private static async Task WriteReportAsync(
        int score, int critical, int major, int minor,
        List<GovernanceRuleResult> rules)
    {
        var report = new
        {
            page         = "/broadcasting",
            device       = "Desktop",
            timestamp    = DateTime.UtcNow,
            score,
            criticalCount = critical,
            majorCount    = major,
            minorCount    = minor,
            rules,
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented  = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var outPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults", "governance-report.json"));

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        await File.WriteAllTextAsync(outPath, json);
    }
}
