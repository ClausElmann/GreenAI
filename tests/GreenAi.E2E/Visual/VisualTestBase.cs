using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using GreenAi.E2E.Assertions;

namespace GreenAi.E2E.Visual;

using GreenAi.E2E; // SharedAuth + LoginTokens

/// <summary>
/// Base class for multi-device visual tests.
/// Independent lifecycle from E2ETestBase ÔÇö creates a fresh Playwright browser per
/// test class, then one BrowserContext per device within each test method.
///
/// Screenshot organisation:
///   TestResults/Visual/baseline/{device}/{test}.png  ÔÇö reference images
///   TestResults/Visual/current/{device}/{test}.png   ÔÇö latest run output
///
/// First run: no baseline ÔåÆ current screenshot IS saved as baseline automatically.
/// Update baselines (after intentional UI change):
///   In terminal (inline ÔÇö no .ps1 file):
///   $env:GREENAI_UPDATE_BASELINE="true"
///   dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" --nologo
///   $env:GREENAI_UPDATE_BASELINE=$null
///
/// Pixel-diff comparison: NOT yet implemented (UI pre-stable phase).
/// Baselines are saved and ready. Add SixLabors.ImageSharp when UI is stable
/// and add CompareWithBaselineAsync() to this class.
/// </summary>
[Collection("E2E")]
public abstract class VisualTestBase : IAsyncLifetime
{
    protected const string BaseUrl = "http://localhost:5057";

    private static readonly string VisualRoot = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "TestResults", "Visual");

    private IPlaywright _playwright = null!;
    private IBrowser    _browser    = null!;

    // ÔöÇÔöÇ Lifecycle ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        // HEADLESS NOTE: Blazor Server uses SignalR WebSockets for its circuit.
        // Headless Chromium shell has known issues with localhost WebSocket
        // connections ÔÇö the circuit fails to establish and @onclick handlers are
        // never wired. Running with Headless=false (same as E2ETestBase) is the
        // reliable fix for local runs. CI can use a virtual display (Xvfb) if
        // needed; for now green-ai CI does not run E2E tests (LocalDB unavailable).
        var isHeadless = Environment.GetEnvironmentVariable("GREENAI_VISUAL_HEADLESS") is "true";
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = isHeadless,
            SlowMo   = isHeadless ? 0 : 80, // small SlowMo keeps Blazor circuit in sync locally
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    // ÔöÇÔöÇ Multi-device runner ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    /// <summary>
    /// Runs <paramref name="testAction"/> once per device in <see cref="DeviceProfile.All"/>.
    /// Each device gets a fresh BrowserContext that is disposed after the action.
    /// All failures are collected and reported together so every device is exercised.
    /// </summary>
    protected async Task ForEachDeviceAsync(
        Func<IPage, DeviceProfile, Task> testAction,
        [CallerMemberName] string         callerName = "")
    {
        var failures = new List<string>();

        foreach (var device in DeviceProfile.All)
        {
            IBrowserContext? ctx  = null;
            IPage?           page = null;
            try
            {
                ctx  = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize      = new ViewportSize { Width = device.Width, Height = device.Height },
                    IsMobile          = device.IsMobile,
                    IgnoreHTTPSErrors = true,
                });
                page = await ctx.NewPageAsync();

                await testAction(page, device);

                // Auto-check: error state + UI quality gates (focus, touch targets, padding,
                // design tokens). A11y gate (axe) is opt-in via GREENAI_ACCESSIBILITY_GATES=true.
                await AssertNoVisibleErrorsAsync(page, device);
                await RunQualityGatesAsync(page, device);
            }
            catch (Exception ex)
            {
                // Capture a diagnostic screenshot before moving to the next device.
                await CaptureErrorAsync(page, device, callerName);
                failures.Add($"[{device.Name} {device.Width}├ù{device.Height}] {ex.Message}");
            }
            finally
            {
                if (page is not null) await page.CloseAsync();
                if (ctx  is not null) await ctx.CloseAsync();
            }
        }

        if (failures.Count > 0)
            throw new Exception(
                $"Visual test failed on {failures.Count}/{DeviceProfile.All.Count} device(s):\n" +
                string.Join("\n", failures));
    }

    // ÔöÇÔöÇ Failure diagnostics ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    /// <summary>
    /// Best-effort diagnostic screenshot taken when a test fails on a device.
    /// Saved alongside normal screenshots as <c>{callerName}-error.png</c>.
    /// Any exception is swallowed ÔÇö the error capture must never mask the original failure.
    /// </summary>
    private async Task CaptureErrorAsync(IPage? page, DeviceProfile device, string callerName)
    {
        if (page is null) return;
        try
        {
            var safe = Sanitize(callerName);
            var dir  = Path.Combine(VisualRoot, "current", device.FolderName);
            Directory.CreateDirectory(dir);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path     = Path.Combine(dir, $"{safe}-error.png"),
                FullPage = true,
            });
        }
        catch
        {
            // Best-effort ÔÇö never propagate from error handler.
        }
    }

    // ÔöÇÔöÇ Auth ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    /// <summary>
    /// Injects a cached JWT into this page's localStorage and navigates to /dashboard.
    /// Tokens are acquired ONCE per test process (see SharedAuth) so no HTTP call per test.
    /// </summary>
    protected async Task LoginAsync(
        IPage  page,
        string email    = "claus.elmann@gmail.com",
        string password = "Flipper12#")
    {
        // Use cached tokens for the primary dev account.
        LoginTokens tokens;
        if (email == "claus.elmann@gmail.com" && password == "Flipper12#")
        {
            tokens = await SharedAuth.PrimaryAsync();
        }
        else
        {
            using var http = new HttpClient();
            var body = System.Text.Json.JsonSerializer.Serialize(new { email, password });
            var response = await http.PostAsync(
                $"{BaseUrl}/api/auth/login",
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Login API returned {response.StatusCode} for {email}.");

            var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            tokens = new LoginTokens(
                json.GetProperty("accessToken").GetString()!,
                json.GetProperty("refreshToken").GetString()!,
                json.GetProperty("expiresAt").GetString()!);
        }

        await page.GotoAsync($"{BaseUrl}/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.EvaluateAsync($"""
            localStorage.setItem('greenai_access_token',  '{tokens.AccessToken}');
            localStorage.setItem('greenai_refresh_token', '{tokens.RefreshToken}');
            localStorage.setItem('greenai_expires_at',    '{tokens.ExpiresAt}');
        """);

        await page.GotoAsync($"{BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }


    // ÔöÇÔöÇ Screenshots ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    /// <summary>
    /// Waits for the page to reach a visually stable state, then takes a full-page screenshot.
    /// Stable = NetworkIdle + all CSS animations/transitions have settled (100ms grace period).
    /// Saves to current/{device}/{testName}.png, and to baseline/ on first run or when
    /// GREENAI_UPDATE_BASELINE=true.
    /// </summary>
    protected async Task CaptureAsync(IPage page, DeviceProfile device, string testName)
    {
        // Stability wait: network must be idle, then a tiny settle for CSS animations.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(100);

        var safe         = Sanitize(testName);
        var currentDir   = Path.Combine(VisualRoot, "current",  device.FolderName);
        var baselineDir  = Path.Combine(VisualRoot, "baseline", device.FolderName);
        var currentPath  = Path.Combine(currentDir,  $"{safe}.png");
        var baselinePath = Path.Combine(baselineDir, $"{safe}.png");

        Directory.CreateDirectory(currentDir);
        Directory.CreateDirectory(baselineDir);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path     = currentPath,
            FullPage = true,
        });

        var updateMode = Environment.GetEnvironmentVariable("GREENAI_UPDATE_BASELINE") is "true";
        if (!File.Exists(baselinePath) || updateMode)
            File.Copy(currentPath, baselinePath, overwrite: true);
    }

    // ÔöÇÔöÇ Layout assertions ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    /// <summary>
    /// Asserts that no horizontal scroll overflow exists on this page.
    /// Horizontal overflow is always a layout regression ÔÇö especially on mobile.
    /// Tolerance of 2px for sub-pixel rendering artefacts.
    /// </summary>
    protected async Task AssertNoHorizontalOverflowAsync(IPage page, DeviceProfile device)
    {
        var overflow = await page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - window.innerWidth)");

        if (overflow > 2)
            throw new Exception(
                $"Horizontal overflow: scrollWidth exceeds innerWidth by {overflow}px on {device.Name} " +
                $"({device.Width}├ù{device.Height}) at {page.Url}. " +
                "Check for elements wider than viewport or missing max-width/overflow-x:hidden.");
    }

    /// <summary>
    /// Asserts that the top-bar and main content are not overlapping.
    /// Uses bounding-box coordinates and padding; fails if visible content starts behind app bar.
    /// MudBlazor MudMainContent uses position:static with padding-top = appbar height, so
    /// getBoundingClientRect().top is 0 (correct) while content is visually below the bar.
    /// We measure paddingTop of the element to check that the content offset is sufficient.
    /// </summary>
    protected async Task AssertTopBarNotClippingContentAsync(IPage page, DeviceProfile device)
    {
        var topBarBottom = await page.EvaluateAsync<double>("""
            () => {
                const el = document.querySelector('[data-testid="top-bar"]');
                return el ? el.getBoundingClientRect().bottom : -1;
            }
            """);

        if (topBarBottom < 0)
            return; // top-bar not present on this page (e.g. EmptyLayout) ÔÇö skip

        // MudMainContent uses padding-top = appbar height ÔÇö check paddingTop ÔëÑ topBarBottom.
        // getBoundingClientRect().top is 0 (element is in normal flow, not offset by the fixed bar).
        var paddingTop = await page.EvaluateAsync<double>("""
            () => {
                const el = document.querySelector('.mud-main-content');
                if (!el) return -1;
                return parseFloat(getComputedStyle(el).paddingTop) || 0;
            }
            """);

        if (paddingTop < 0)
            return; // mud-main-content not present ÔÇö skip

        if (paddingTop < topBarBottom - 4)
            throw new Exception(
                $"MudMainContent paddingTop ({paddingTop:F0}px) is less than TopBar bottom ({topBarBottom:F0}px) on {device.Name} ÔÇö " +
                "main content is being clipped by the app bar.");
    }

    /// <summary>
    /// Asserts that no interactive element (button, link, role=button) with a non-zero
    /// bounding rect is positioned more than 20 px outside the viewport horizontally.
    /// Catches the CSS anti-pattern of hiding elements at <c>left: -9999px</c> and
    /// elements that overflow the viewport edge on narrow screens.
    /// </summary>
    protected async Task AssertInteractiveElementsVisibleAsync(IPage page, DeviceProfile device)
    {
        var offscreen = await page.EvaluateAsync<string[]>("""
            () => {
                const vw  = window.innerWidth;
                const sel = 'button, a[href], [role="button"]';
                const out = [];
                for (const el of document.querySelectorAll(sel)) {
                    const r = el.getBoundingClientRect();
                    if (r.width === 0 || r.height === 0) continue; // legitimately hidden
                    const id = el.getAttribute('data-testid')
                             ?? el.textContent?.trim().slice(0, 30)
                             ?? el.tagName.toLowerCase();
                    if (r.left < -20)
                        out.push(`left: "${id}" at x=${r.left.toFixed(0)}`);
                    if (r.right > vw + 20)
                        out.push(`right: "${id}" at x=${r.right.toFixed(0)} (vw=${vw})`);
                }
                return out;
            }
            """);

        if (offscreen.Length > 0)
            throw new Exception(
                $"[{device.Name}] {offscreen.Length} interactive element(s) are positioned off-screen:\n" +
                string.Join("\n", offscreen.Take(5)) +
                (offscreen.Length > 5 ? $"\n\u2026and {offscreen.Length - 5} more." : ""));
    }

    /// <summary>
    /// Asserts that no interactive element (button, link) is obscured by an overlapping
    /// element at its centre point. Uses <c>document.elementFromPoint</c> to detect
    /// accidental full-page overlays or mis-stacked z-indexes.
    /// Only checks elements whose centre lies within the current viewport.
    /// </summary>
    protected async Task AssertNoOverlappingClickableElementsAsync(IPage page, DeviceProfile device)
    {
        var blocked = await page.EvaluateAsync<string[]>("""
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
                $"[{device.Name}] {blocked.Length} interactive element(s) are obscured by overlapping elements:\n" +
                string.Join("\n", blocked.Take(5)) +
                (blocked.Length > 5 ? $"\n\u2026and {blocked.Length - 5} more." : ""));
    }

    // ÔöÇÔöÇ Private helpers ÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇÔöÇ

    /// <summary>
    /// Asserts layout consistency: every element whose bounding rect has non-zero size
    /// must fit within the current viewport width (allowing 2px sub-pixel tolerance).
    /// Overlap on the right edge is the most common symptom of missing
    /// <c>max-width: 100%</c>, <c>box-sizing: border-box</c>, or negative margins on mobile.
    /// Only scans the first 300 elements to keep test time bounded.
    /// </summary>
    protected async Task AssertLayoutConsistencyAsync(IPage page, DeviceProfile device)
    {
        var violations = await page.EvaluateAsync<string[]>("""
            () => {
                const vw  = window.innerWidth;
                const out = [];
                let   n   = 0;
                for (const el of document.querySelectorAll('*')) {
                    if (++n > 300) break;
                    const r = el.getBoundingClientRect();
                    if (r.width === 0) continue;
                    if (r.right > vw + 2) {
                        const id = el.getAttribute('data-testid')
                                 ?? el.id
                                 ?? el.className?.split(' ').slice(0,3).join('.')
                                 ?? el.tagName;
                        out.push(`"${id}" right=${r.right.toFixed(0)} (vw=${vw})`);
                    }
                }
                return [...new Set(out)];
            }
            """);

        if (violations.Length > 0)
            throw new Exception(
                $"[{device.Name}] {violations.Length} element(s) exceed viewport width:\n" +
                string.Join("\n", violations.Take(5)) +
                (violations.Length > 5 ? $"\n\u2026and {violations.Length - 5} more." : ""));
    }

    /// <summary>
    /// Asserts that no rendered element has extreme spacing (margin/padding > 200px)
    /// or a suspiciously tiny height that suggests a collapsed container (1\u20138px).
    /// Both are reliable signals of CSS regressions that make the UI visually broken
    /// without necessarily causing horizontal overflow.
    /// </summary>
    protected async Task AssertReasonableSpacingAsync(IPage page, DeviceProfile device)
    {
        var issues = await page.EvaluateAsync<string[]>("""
            () => {
                const out = [];
                let n = 0;
                for (const el of document.querySelectorAll('*')) {
                    if (++n > 300) break;
                    const r = el.getBoundingClientRect();
                    if (r.width === 0 || r.height === 0) continue; // display:none / truly invisible
                    // Skip MudBlazor internal elements (their rendering uses thin sizing
                    // internally ÔÇö popovers, slots, adornments) and SVG/HR structural elements.
                    // We only want to catch anomalies in our own layout containers.
                    const cls = typeof el.className === 'string' ? el.className : '';
                    const tag = el.tagName;
                    const eid = el.id ?? '';
                    if (cls.includes('mud-') || eid.startsWith('mud') || tag === 'HR' || tag === 'PATH' ||
                        tag === 'SVG'        || tag === 'STYLE' || tag === 'SCRIPT')
                        continue;
                    const cs = window.getComputedStyle(el);
                    const id = el.getAttribute('data-testid')
                             ?? el.id
                             ?? el.tagName.toLowerCase();
                    // Extreme spacing ÔÇö flag margin/padding > 200px on non-MudBlazor elements
                    const props = ['marginTop','marginBottom','marginLeft','marginRight',
                                   'paddingTop','paddingBottom','paddingLeft','paddingRight'];
                    for (const p of props) {
                        const v = parseFloat(cs[p]);
                        if (v > 200)
                            out.push(`"${id}" has ${p}=${v}px`);
                    }
                    // Collapsed container: visible in DOM but ~invisible in height
                    if (r.height > 0 && r.height < 4 && el.children.length > 0)
                        out.push(`"${id}" collapsed: height=${r.height.toFixed(1)}px with ${el.children.length} child(ren)`);
                }
                return [...new Set(out)];
            }
            """);

        if (issues.Length > 0)
            throw new Exception(
                $"[{device.Name}] {issues.Length} spacing/size anomaly(s) detected:\n" +
                string.Join("\n", issues.Take(5)) +
                (issues.Length > 5 ? $"\n\u2026and {issues.Length - 5} more." : ""));
    }

    /// <summary>
    /// Asserts that navigation is usable on the current page:
    /// <list type="bullet">
    /// <item>Top-bar toggle button exists and is not covered.</item>
    /// <item>If the overlay-nav panel is open, it contains at least one visible link.</item>
    /// </list>
    /// Does <em>not</em> open the panel ÔÇö call after interactions that already opened it,
    /// or use on the closed state to verify the toggle is reachable.
    /// </summary>
    protected async Task AssertNavigationUsableAsync(IPage page, DeviceProfile device)
    {
        // 1. Toggle button must exist and be visible
        var toggleRect = await page.EvaluateAsync<double[]>("""
            () => {
                const el = document.querySelector('[data-testid="top-bar-nav-toggle"]');
                if (!el) return [0, 0, 0, 0];
                const r = el.getBoundingClientRect();
                return [r.width, r.height, r.left, r.top];
            }
            """);

        if (toggleRect[0] == 0 || toggleRect[1] == 0)
            throw new Exception(
                $"[{device.Name}] Nav toggle button (data-testid='top-bar-nav-toggle') is not visible "
                + $"at {page.Url}.");

        // 2. If overlay-nav panel is already open, it must contain clickable links
        var navLinkCount = await page.EvaluateAsync<int>("""
            () => {
                const panel = document.querySelector('[data-testid="overlay-nav-panel"]');
                if (!panel) return -1; // panel not open ÔÇö skip
                const r = panel.getBoundingClientRect();
                if (r.width === 0 || r.height === 0) return -1; // closed/hidden
                return panel.querySelectorAll('a[href], [role="menuitem"], .mud-nav-link').length;
            }
            """);

        if (navLinkCount == 0)
            throw new Exception(
                $"[{device.Name}] Overlay-nav panel is open but contains 0 clickable links.");
    }

    /// <summary>
    /// Asserts that no text-bearing element has both <c>overflow:hidden</c>/<c>text-overflow:ellipsis</c>
    /// and a <c>scrollWidth</c> that exceeds its <c>clientWidth</c> by more than 4px.
    /// That combination means text is actively being cut off ÔÇö a sign that labels are too long
    /// for their allocated space or that the container has no minimum width.
    /// Only checks the first 200 visible text-bearing elements to bound execution time.
    /// </summary>
    protected async Task AssertNoTextOverflowAsync(IPage page, DeviceProfile device)
    {
        var clipped = await page.EvaluateAsync<string[]>("""
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
                        const cs     = window.getComputedStyle(el);
                        const hasClip = cs.overflow === 'hidden' || cs.overflowX === 'hidden';
                        const hasEllipsis = cs.textOverflow === 'ellipsis';
                        // scrollWidth will exceed clientWidth when text is truncated
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
                $"[{device.Name}] {clipped.Length} text element(s) have overflowing/clipped text:\n" +
                string.Join("\n", clipped.Take(5)) +
                (clipped.Length > 5 ? $"\n\u2026and {clipped.Length - 5} more." : ""));
    }

    /// <summary>
    /// Asserts that no visible error states exist on the current page:
    /// <list type="bullet">
    ///   <item>Blazor circuit error overlay (<c>#blazor-error-ui</c>) is not visible.</item>
    ///   <item>No MudBlazor error-severity alerts are rendered.</item>
    ///   <item>No Blazor component-level error boundary is triggered.</item>
    /// </list>
    /// Called automatically by <see cref="ForEachDeviceAsync"/> after each test action
    /// so screenshots are never silently capturing backend error UI.
    /// </summary>
    protected async Task AssertNoVisibleErrorsAsync(IPage page, DeviceProfile device)
    {
        // 1. Blazor circuit error overlay (appears on unhandled circuit exception)
        var blazorErrorVisible = await page.EvaluateAsync<bool>("""
            () => {
                const el = document.getElementById('blazor-error-ui');
                if (!el) return false;
                const cs = window.getComputedStyle(el);
                return cs.display !== 'none' && cs.visibility !== 'hidden' && el.offsetHeight > 0;
            }
            """);

        if (blazorErrorVisible)
            throw new Exception(
                $"[{device.Name}] Blazor circuit error overlay (#blazor-error-ui) is visible on {page.Url}. "
                + "A backend exception or SignalR circuit failure occurred.");

        // 2. Visible MudBlazor error alerts (Severity.Error)
        var mudErrors = await page.EvaluateAsync<string[]>("""
            () => {
                const sel = '.mud-alert-filled-error, .mud-alert-outlined-error, .mud-alert-text-error';
                const out = [];
                for (const el of document.querySelectorAll(sel)) {
                    const r = el.getBoundingClientRect();
                    if (r.width > 0 && r.height > 0)
                        out.push((el.textContent?.trim() ?? '').slice(0, 100));
                }
                return out;
            }
            """);

        if (mudErrors.Length > 0)
            throw new Exception(
                $"[{device.Name}] {mudErrors.Length} visible error alert(s) on {page.Url}:\n"
                + string.Join("\n", mudErrors.Select(m => $"  ÔÇó {m}")));

        // 3. Blazor component-level error boundary (renders fallback UI on component exceptions)
        var errorBoundaryVisible = await page.EvaluateAsync<bool>("""
            () => {
                for (const el of document.querySelectorAll('[data-blazor-error-boundary]')) {
                    const r = el.getBoundingClientRect();
                    if (r.width > 0 && r.height > 0) return true;
                }
                return false;
            }
            """);

        if (errorBoundaryVisible)
            throw new Exception(
                $"[{device.Name}] A Blazor error boundary is displaying fallback UI on {page.Url}. "
                + "Check server logs for the component exception.");
    }

    // ── UI quality gates (auto-run inside ForEachDeviceAsync) ─────────────────────────

    /// <summary>Runs design-token + layout quality gates after each test action.</summary>
    private async Task RunQualityGatesAsync(IPage page, DeviceProfile device)
    {
        // Skip all quality gates if the page hasn't navigated (e.g. about:blank in
        // tests that close a dialog/nav without an explicit next navigation).
        if (page.Url is "about:blank" or "")
            return;

        await DesignSystemAssertions.AssertTokensDefinedAsync(page, $"{device.Name}/{page.Url}");
        await DesignSystemAssertions.AssertSpacingScaleAsync(page, $"{device.Name}/{page.Url}");
        await AssertFocusVisibleAsync(page, device);
        await AssertMinimumTouchTargetAsync(page, device);
        await AssertContentHasSafePaddingAsync(page, device);

        var a11yMode = Environment.GetEnvironmentVariable("GREENAI_ACCESSIBILITY_GATES");
        if (a11yMode is "true" or "strict")
            await AccessibilityAssertions.AssertNoViolationsAsync(page, $"{device.Name}/{page.Url}");
    }

    /// <summary>
    /// Asserts that the global focus ring fires on keyboard focus:
    /// presses Tab once and verifies the active element's computed outline-width is >= 2px.
    /// Skips if no focusable elements are present (e.g. empty page).
    /// </summary>
    protected async Task AssertFocusVisibleAsync(IPage page, DeviceProfile device)
    {
        var hasFocusable = await page.EvaluateAsync<bool>("""
            () => {
                return !!document.querySelector('button:not([disabled]):not([tabindex="-1"]), a[href]:not([tabindex="-1"]), input:not([type="hidden"]):not([disabled])');
            }
            """);

        if (!hasFocusable) return;

        await page.Keyboard.PressAsync("Tab");

        var outlineWidth = await page.EvaluateAsync<double>("""
            () => {
                const el = document.activeElement;
                if (!el || el === document.body) return -1;
                return parseFloat(window.getComputedStyle(el).outlineWidth) || 0;
            }
            """);

        if (outlineWidth == -1) return; // no element focused — skip

        if (outlineWidth < 2)
            throw new Exception(
                $"[{device.Name}] Focus ring too thin or missing: outline-width={outlineWidth}px on "
                + $"{page.Url} (spec: 3px solid var(--ga-focus)).");
    }

    /// <summary>
    /// On mobile profiles (<c>IsMobile = true</c>), asserts that all visible interactive
    /// elements meet the WCAG 2.5.5 minimum touch target of 44x44px.
    /// Skipped on desktop where 40px minimum applies.
    /// </summary>
    protected async Task AssertMinimumTouchTargetAsync(IPage page, DeviceProfile device)
    {
        if (!device.IsMobile) return;

        var violations = await page.EvaluateAsync<string[]>("""
            () => {
                const MIN = 44;
                const out = [];
                const selectors = [
                    'button:not([disabled])',
                    'a[href]:not([tabindex="-1"])',
                    '[role="button"]:not([disabled])',
                    'input[type="checkbox"]',
                    'input[type="radio"]',
                ];
                for (const sel of selectors) {
                    for (const el of document.querySelectorAll(sel)) {
                        const cls = typeof el.className === 'string' ? el.className : '';
                        if (cls.includes('mud-icon-root')) continue;
                        const r = el.getBoundingClientRect();
                        if (r.width === 0 || r.height === 0) continue;
                        if (r.width < MIN || r.height < MIN) {
                            const id = el.getAttribute('data-testid')
                                     || (el.textContent || '').trim().slice(0, 30)
                                     || el.tagName;
                            out.push('"' + id + '" ' + r.width.toFixed(0) + 'x' + r.height.toFixed(0) + 'px (< ' + MIN + 'px)');
                        }
                    }
                }
                return out.slice(0, 10);
            }
            """);

        if (violations is { Length: > 0 })
            throw new Exception(
                $"[{device.Name}] {violations.Length} touch target(s) below 44x44px minimum:\n"
                + string.Join("\n", violations.Select(v => $"  - {v}")));
    }

    /// <summary>
    /// Asserts that the main content area has safe horizontal padding (>= 12px mobile,
    /// >= 16px desktop) so content never hugs the screen edge.
    /// </summary>
    protected async Task AssertContentHasSafePaddingAsync(IPage page, DeviceProfile device)
    {
        // Pass minPadding as a JS argument to avoid $""" / {{ brace escaping issues
        var minPadding = device.IsMobile ? 12 : 16;

        var result = await page.EvaluateAsync<string?>("""
            (minPx) => {
                const selectors = [
                    '.mud-main-content .main-content-inner',
                    '.ga-form-page',
                    '.ga-form-card',
                    '.select-context-page',
                ];
                for (const sel of selectors) {
                    const el = document.querySelector(sel);
                    if (!el) continue;
                    const r = el.getBoundingClientRect();
                    if (r.width === 0) continue;
                    const cs = window.getComputedStyle(el);
                    const pL = parseFloat(cs.paddingLeft)  || 0;
                    const pR = parseFloat(cs.paddingRight) || 0;
                    if (pL < minPx || pR < minPx) {
                        return '"' + sel + '" paddingLeft=' + pL + 'px paddingRight=' + pR + 'px (< ' + minPx + 'px)';
                    }
                }
                return null;
            }
            """, minPadding);

        if (result is not null)
            throw new Exception(
                $"[{device.Name}] Content safe-padding violation on '{page.Url}': {result}");
    }

    // ── Cross-browser runner ────────────────────────────────────────────────────

    /// <summary>
    /// Runs <paramref name="testAction"/> against each browser+device combination.
    /// Browsers controlled by <c>GREENAI_BROWSERS</c> env var (comma-separated):
    ///   <c>chromium</c> (default), <c>webkit</c>, <c>firefox</c>.
    /// Quality gates run automatically for every browser.
    /// </summary>
    protected async Task ForEachBrowserAsync(
        Func<IPage, DeviceProfile, Task> testAction,
        [CallerMemberName] string         callerName = "")
    {
        var browserNames = (Environment.GetEnvironmentVariable("GREENAI_BROWSERS") ?? "chromium")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var failures = new List<string>();

        foreach (var browserName in browserNames)
        {
            IBrowser? extraBrowser = null;
            try
            {
                IBrowser browser = browserName.ToLowerInvariant() switch
                {
                    "webkit"  => extraBrowser = await _playwright.Webkit.LaunchAsync(
                                    new BrowserTypeLaunchOptions { Headless = false }),
                    "firefox" => extraBrowser = await _playwright.Firefox.LaunchAsync(
                                    new BrowserTypeLaunchOptions { Headless = false }),
                    _         => _browser,
                };

                foreach (var device in DeviceProfile.All)
                {
                    IBrowserContext? ctx  = null;
                    IPage?           page = null;
                    try
                    {
                        ctx  = await browser.NewContextAsync(new BrowserNewContextOptions
                        {
                            ViewportSize      = new ViewportSize { Width = device.Width, Height = device.Height },
                            IsMobile          = device.IsMobile,
                            IgnoreHTTPSErrors = true,
                        });
                        page = await ctx.NewPageAsync();

                        await testAction(page, device);

                        await AssertNoVisibleErrorsAsync(page, device);
                        await RunQualityGatesAsync(page, device);
                    }
                    catch (Exception ex)
                    {
                        await CaptureErrorAsync(page, device, $"{browserName}-{callerName}");
                        failures.Add($"[{browserName}/{device.Name}] {ex.Message}");
                    }
                    finally
                    {
                        if (page is not null) await page.CloseAsync();
                        if (ctx  is not null) await ctx.CloseAsync();
                    }
                }
            }
            finally
            {
                if (extraBrowser is not null) await extraBrowser.DisposeAsync();
            }
        }

        if (failures.Count > 0)
            throw new Exception(
                $"Cross-browser test failed on {failures.Count} combination(s):\n" +
                string.Join("\n", failures));
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_'));
}
