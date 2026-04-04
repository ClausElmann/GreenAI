using Microsoft.Data.SqlClient;
using Microsoft.Playwright;

namespace GreenAi.E2E;

/// <summary>
/// Base class for all E2E tests. Creates a fresh browser page per test.
/// Requires the app running on http://localhost:5057 before running tests.
///
/// DEBUG HELPERS (see docs/SSOT/testing/debug-protocol.md):
///   - Browser console errors are captured throughout the test
///   - FailAsync(reason) → screenshot + URL + console errors + DB logs → throws
///   - WaitOrFailAsync(selector) → calls FailAsync on timeout
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected const string BaseUrl = "http://localhost:5057";

    private const string DbConnection =
        @"Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Integrated Security=true;TrustServerCertificate=true;";

    private static readonly string ScreenshotDir =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults", "Screenshots");

    private IPlaywright _playwright = null!;
    private IBrowser    _browser    = null!;
    private readonly List<string> _consoleErrors = [];

    protected IPage Page { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo   = 150
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        Page = await context.NewPageAsync();

        // Capture browser console errors throughout the test
        Page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
                _consoleErrors.Add($"[{msg.Type.ToUpper()}] {msg.Text}");
        };
        Page.PageError += (_, error) => _consoleErrors.Add($"[PAGE_ERROR] {error}");
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    // -------------------------------------------------------------------------
    // Debug helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Waits for a CSS selector. On timeout: takes screenshot, collects console errors
    /// and recent DB logs, then throws with complete diagnostic context.
    /// </summary>
    protected async Task WaitOrFailAsync(string selector, int timeoutMs = 10_000, string? hint = null)
    {
        try
        {
            await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = timeoutMs });
        }
        catch (TimeoutException)
        {
            await FailAsync($"Timeout waiting for '{selector}'" + (hint is not null ? $" — {hint}" : ""));
        }
    }

    /// <summary>
    /// Takes a screenshot, collects browser console errors and recent DB logs,
    /// then throws an exception with full diagnostic context.
    /// Call this from any test that encounters an unexpected state.
    /// </summary>
    protected async Task FailAsync(string reason)
    {
        // 1. Screenshot
        var screenshotPath = await TakeScreenshotAsync();

        // 2. DB logs (last 15 rows since test started — covers the test window)
        var dbLogs = await FetchRecentDbLogsAsync();

        // 3. Console errors collected during the test
        var console = _consoleErrors.Count > 0
            ? string.Join("\n  ", _consoleErrors)
            : "(none)";

        throw new Exception(
            $"""
            E2E FAILURE: {reason}

            URL:     {Page.Url}
            Screenshot: {screenshotPath}

            Browser console errors:
              {console}

            Recent DB logs (Logs table):
            {dbLogs}
            """);
    }

    // -------------------------------------------------------------------------
    // Auth helpers
    // -------------------------------------------------------------------------

    /// <summary>Logs in with dev credentials and waits until redirected away from /login.</summary>
    protected async Task LoginAsync(string email = "admin@dev.local", string password = "dev123")
    {
        await Page.GotoAsync($"{BaseUrl}/login");

        await Page.WaitForSelectorAsync("input[type='email']", new PageWaitForSelectorOptions { Timeout = 10_000 });

        await Page.FillAsync("input[type='email']", email);
        await Page.FillAsync("input[type='password']", password);
        await Page.ClickAsync("button[type='submit']");

        // Blazor Server uses WebSocket — poll for URL change
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && Page.Url.Contains("/login"))
            await Task.Delay(200, TestContext.Current.CancellationToken);

        if (Page.Url.Contains("/login"))
            await FailAsync($"Login did not redirect away from /login after 15s (email={email})");
    }

    // -------------------------------------------------------------------------
    // Label validation helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scans the rendered page for missing localization labels.
    /// Missing labels appear as raw keys in the UI: "shared.SaveButton" instead of "Gem".
    /// Uses JavaScript to traverse all text nodes recursively.
    /// </summary>
    protected async Task<IReadOnlyList<string>> ScanForMissingLabelsAsync()
    {
        // Wait for network idle so dynamic labels have time to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        const string js = """
            (() => {
                const missing = new Set();
                const pattern = /\b(shared|feature|nav)\.[A-Z][a-zA-Z0-9]+/g;

                function scan(node) {
                    if (node.nodeType === 3) {
                        const text = node.textContent.trim();
                        if (text) {
                            const hits = text.match(pattern);
                            if (hits) hits.forEach(h => missing.add(h));
                        }
                    } else {
                        node.childNodes.forEach(scan);
                    }
                }

                scan(document.body);
                return Array.from(missing);
            })()
            """;

        var result = await Page.EvaluateAsync<string[]>(js);
        return result ?? [];
    }

    /// <summary>
    /// Asserts that the current page has NO raw label keys visible in the UI.
    /// Call this after navigating to a page to verify all localization labels loaded correctly.
    /// </summary>
    protected async Task AssertNoMissingLabelsAsync(string? pageDescription = null)
    {
        var missing = await ScanForMissingLabelsAsync();

        if (missing.Count > 0)
        {
            var context = pageDescription ?? Page.Url;
            var details = string.Join("\n  - ", missing);
            await FailAsync(
                $"Label validation failed on '{context}' — {missing.Count} raw key(s) found:\n  - {details}\n\n" +
                "Fix: run scripts/localization/Add-Labels.ps1 for missing keys (DA + EN).");
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<string> TakeScreenshotAsync()
    {
        try
        {
            Directory.CreateDirectory(ScreenshotDir);
            var name = $"{GetType().Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var path = Path.Combine(ScreenshotDir, name);
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            return path;
        }
        catch
        {
            return "(screenshot failed)";
        }
    }

    private static async Task<string> FetchRecentDbLogsAsync()
    {
        try
        {
            await using var conn = new SqlConnection(DbConnection);
            await conn.OpenAsync();

            const string sql = """
                SELECT TOP 15
                    CONVERT(varchar(23), TimeStamp, 121) AS [Time],
                    Level,
                    LEFT(Message, 200) AS Message,
                    LEFT(Exception, 300) AS Exception
                FROM [dbo].[Logs]
                ORDER BY TimeStamp DESC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                var time      = reader["Time"]?.ToString() ?? "";
                var level     = reader["Level"]?.ToString() ?? "";
                var message   = reader["Message"]?.ToString()?.Trim() ?? "";
                var exception = reader["Exception"]?.ToString()?.Trim();

                rows.Add(exception is { Length: > 0 }
                    ? $"  [{level,7}] {time}  {message}\n           EX: {exception}"
                    : $"  [{level,7}] {time}  {message}");
            }

            return rows.Count > 0
                ? string.Join("\n", rows)
                : "  (no rows in Logs table)";
        }
        catch (Exception ex)
        {
            return $"  (DB query failed: {ex.Message})";
        }
    }
}

