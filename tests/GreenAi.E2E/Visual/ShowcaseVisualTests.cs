using Microsoft.Playwright;

namespace GreenAi.E2E.Visual;

/// <summary>
/// Visual tests for the /ui-showcase page.
///
/// The showcase page covers ALL MudBlazor components used in green-ai so we
/// can validate rendering before backend implementation.
///
/// Screenshots per section (all 4 device profiles):
///   showcase-kpi              — KPI stat cards
///   showcase-users-tab        — User table with search (default tab)
///   showcase-messages-tabs    — Nested tabs (Sendte/Planlagte/Fejlede/Tom)
///   showcase-inner-scheduled  — Nested tab: Planlagte active
///   showcase-inner-failed     — Nested tab: Fejlede active
///   showcase-inner-empty      — Nested tab: Tom tilstand (empty state)
///   showcase-activity-tab     — Activity feed tab
///   showcase-forms            — SMS settings form + validation error form
///   showcase-alerts           — All alert severities + skeleton
///   showcase-chips            — Status chips + count chips
///   showcase-buttons          — Button rows (filled, outlined, icon)
///   showcase-empty-states     — 3 empty state variants side by side
///   showcase-typography       — Typography hierarchy + breadcrumbs
///   showcase-delete-dialog    — Slet-dialog open
///   showcase-create-dialog    — Opret bruger dialog open
/// </summary>
public sealed class ShowcaseVisualTests : VisualTestBase
{
    private const string ShowcaseUrl = $"{BaseUrl}/ui-showcase";

    // ── §1 KPI cards ─────────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_KpiCards_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='kpi-cards']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await ScrollToSection(page, "section-kpi");
            await CaptureAsync(page, device, "showcase-kpi");
        });

    // ── §2 Users tab — table + search ────────────────────────────────────────

    [Fact]
    public Task Showcase_UsersTab_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='showcase-outer-tabs']", new() { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='user-table']", new() { Timeout = 10_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await ScrollToSection(page, "showcase-outer-tabs");
            await CaptureAsync(page, device, "showcase-users-tab");
        });

    [Fact]
    public Task Showcase_UserSearch_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='user-search']", new() { Timeout = 15_000 });

            // Type a search to filter — should show subset of users
            await page.FillAsync("[data-testid='user-search']", "Retail");
            await page.WaitForTimeoutAsync(400); // debounce wait

            await AssertNoHorizontalOverflowAsync(page, device);
            await ScrollToSection(page, "user-search");
            await CaptureAsync(page, device, "showcase-user-search-active");
        });

    // ── §2 Messages — nested tabs ─────────────────────────────────────────────

    [Fact]
    public Task Showcase_NestedTabs_ScheduledActive_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='showcase-outer-tabs']", new() { Timeout = 15_000 });

            // Navigate to Messages tab (index 1 in outer tabs)
            await page.Locator("[data-testid='showcase-outer-tabs'] .mud-tab").Nth(1).ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='showcase-inner-tabs']", new() { Timeout = 8_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "showcase-messages-tab");

            // Switch nested tab to Planlagte (index 1)
            await page.Locator("[data-testid='showcase-inner-tabs'] .mud-tab").Nth(1).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await CaptureAsync(page, device, "showcase-inner-scheduled");
        });

    [Fact]
    public Task Showcase_NestedTabs_FailedAndEmpty_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='showcase-outer-tabs']", new() { Timeout = 15_000 });

            // Navigate to Messages tab
            await page.Locator("[data-testid='showcase-outer-tabs'] .mud-tab").Nth(1).ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='showcase-inner-tabs']", new() { Timeout = 8_000 });

            // Fejlede (index 2)
            await page.Locator("[data-testid='showcase-inner-tabs'] .mud-tab").Nth(2).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await CaptureAsync(page, device, "showcase-inner-failed");

            // Tom tilstand (index 3) — empty state
            await page.Locator("[data-testid='showcase-inner-tabs'] .mud-tab").Nth(3).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await CaptureAsync(page, device, "showcase-inner-empty");
        });

    // ── §2 Activity tab ───────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_ActivityTab_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='showcase-outer-tabs']", new() { Timeout = 15_000 });

            // Activity tab is index 2 in outer tabs
            await page.Locator("[data-testid='showcase-outer-tabs'] .mud-tab").Nth(2).ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='activity-list']", new() { Timeout = 8_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "showcase-activity-tab");
        });

    // ── §3 Forms ──────────────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_Forms_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='section-forms']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertInteractiveElementsVisibleAsync(page, device);
            await ScrollToSection(page, "section-forms");
            await CaptureAsync(page, device, "showcase-forms");
        });

    // ── §4 Alerts + skeleton ──────────────────────────────────────────────────

    [Fact]
    public Task Showcase_Alerts_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='section-alerts']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await ScrollToSection(page, "section-alerts");
            await CaptureAsync(page, device, "showcase-alerts");
        });

    // ── §5 Chips ──────────────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_Chips_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='section-chips']", new() { Timeout = 15_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await ScrollToSection(page, "section-chips");
            await CaptureAsync(page, device, "showcase-chips");
        });

    // ── §6 Buttons ────────────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_Buttons_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='section-buttons']", new() { Timeout = 15_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await ScrollToSection(page, "section-buttons");
            await CaptureAsync(page, device, "showcase-buttons");
        });

    // ── §8 Empty states ───────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_EmptyStates_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='section-empty-states']", new() { Timeout = 15_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await ScrollToSection(page, "section-empty-states");
            await CaptureAsync(page, device, "showcase-empty-states");
        });

    // ── §9 Typography ─────────────────────────────────────────────────────────

    [Fact]
    public Task Showcase_Typography_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='section-typography']", new() { Timeout = 15_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await ScrollToSection(page, "section-typography");
            await CaptureAsync(page, device, "showcase-typography");
        });

    // ── §10 Delete confirm dialog ─────────────────────────────────────────────

    [Fact]
    public Task Showcase_DeleteDialog_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='btn-open-delete-dialog']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.ClickAsync("[data-testid='btn-open-delete-dialog']");
            await page.WaitForSelectorAsync("[data-testid='delete-confirm-dialog']", new() { Timeout = 8_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "showcase-delete-dialog");

            await page.ClickAsync("[data-testid='dialog-cancel']");
        });

    // ── §10 Create user dialog (with form) ────────────────────────────────────

    [Fact]
    public Task Showcase_CreateUserDialog_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='btn-open-create-dialog']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.ClickAsync("[data-testid='btn-open-create-dialog']");
            await page.WaitForSelectorAsync("[data-testid='create-user-dialog']", new() { Timeout = 8_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "showcase-create-dialog");

            // Also capture dialog with validation errors: submit empty → errors shown
            await page.ClickAsync("[data-testid='dialog-confirm-create']");
            await page.WaitForTimeoutAsync(300);
            await CaptureAsync(page, device, "showcase-create-dialog-validation-error");

            // Close: fill email and confirm
            await page.FillAsync("[data-testid='dialog-email']", "test@example.com");
            await page.FillAsync("[data-testid='dialog-name']", "Test Bruger");
            await page.ClickAsync("[data-testid='dialog-confirm-create']");
            await page.WaitForSelectorAsync("[data-testid='create-user-dialog']",
                new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
        });

    // ── §10 Delete dialog directly from user row ──────────────────────────────

    [Fact]
    public Task Showcase_DeleteDialogFromRow_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAndGoto(page);
            await page.WaitForSelectorAsync("[data-testid='user-table']", new() { Timeout = 15_000 });

            // Click delete icon on first row
            var deleteBtn = page.Locator(".mud-icon-button[title='Slet bruger']").First;
            await deleteBtn.ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='delete-confirm-dialog']", new() { Timeout = 8_000 });

            await CaptureAsync(page, device, "showcase-delete-dialog-from-row");

            await page.ClickAsync("[data-testid='dialog-cancel']");
        });

    // ── Full-page scroll (long form on desktop) ───────────────────────────────

    [Fact]
    public Task Showcase_FullPage_Desktop()
        => ForEachDeviceAsync(async (page, device) =>
        {
            if (device.IsMobile) return; // desktop only

            await LoginAndGoto(page);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1_000);

            // Full page screenshot — shows entire showcase in one image
            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "showcase-full-page");
        });

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task LoginAndGoto(IPage page)
    {
        await LoginAsync(page);
        await page.GotoAsync(ShowcaseUrl);
    }

    private static async Task ScrollToSection(IPage page, string testId)
    {
        try
        {
            await page.EvaluateAsync(
                "id => document.querySelector(`[data-testid='${id}']`)?.scrollIntoView({block:'start', behavior:'instant'})",
                testId);
            await page.WaitForTimeoutAsync(200); // allow scroll + repaint
        }
        catch
        {
            // best-effort — never fail the test due to scroll
        }
    }
}
