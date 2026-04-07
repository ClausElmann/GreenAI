using Microsoft.Playwright;

namespace GreenAi.E2E.Visual;

/// <summary>
/// Visual tests for all application pages not covered in NavigationVisualTests.
///
/// Coverage (per page, all 4 device profiles):
///   - DraftsPage             /drafts
///   - StatusPage             /status  (default Sent tab)
///   - StatusPage_ScheduledTab         (Scheduled tab activated)
///   - StatusPage_FailedTab            (Failed tab activated)
///   - StatusDetailPage       /status/1 (stat cards + detail tabs)
///   - StatusDetailPage_ResendDialog   (resend confirmation dialog open)
///   - WizardPage_Step1       /send/wizard  (method selection)
///   - WizardPage_Step2       (recipients step after selecting SMS)
///   - SelectCustomerPage     /select-customer  (select-context-card grid)
///   - SelectProfilePage      /select-profile   (select-context-card grid)
///   - CustomerAdminPage      /customer-admin   (tabs: Users/Profiles/Labels)
///   - AdminUserListPage      /admin/users      (table + filter)
///   - AdminSettingsPage      /admin/settings
/// </summary>
public sealed class PageVisualTests : VisualTestBase
{
    // ── Drafts ────────────────────────────────────────────────────────────────

    [Fact]
    public Task DraftsPage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/drafts");
            await page.WaitForSelectorAsync("[data-testid='drafts-table']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "drafts-page");
        });

    // ── Status: Sent tab (default) ────────────────────────────────────────────

    [Fact]
    public Task StatusPage_SentTab_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/status");
            await page.WaitForSelectorAsync("[data-testid='status-tabs']", new() { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='status-list-table']", new() { Timeout = 10_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "status-sent-tab");
        });

    // ── Status: Scheduled tab ────────────────────────────────────────────────

    [Fact]
    public Task StatusPage_ScheduledTab_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/status");
            await page.WaitForSelectorAsync("[data-testid='status-tabs']", new() { Timeout = 15_000 });

            // MudTabPanel data-testid is on the content div, not the clickable header.
            // Tab headers are .mud-tab buttons inside .mud-tabs-tabbar — click by index.
            await page.Locator("[data-testid='status-tabs'] .mud-tab").Nth(1).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "status-scheduled-tab");
        });

    // ── Status: Failed tab ───────────────────────────────────────────────────

    [Fact]
    public Task StatusPage_FailedTab_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/status");
            await page.WaitForSelectorAsync("[data-testid='status-tabs']", new() { Timeout = 15_000 });

            // Tab header index 2 = Failed tab
            await page.Locator("[data-testid='status-tabs'] .mud-tab").Nth(2).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "status-failed-tab");
        });

    // ── Status Detail ─────────────────────────────────────────────────────────

    [Fact]
    public Task StatusDetailPage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            // ID 1 is always a Sent broadcast in MockData
            await page.GotoAsync($"{BaseUrl}/status/1");
            await page.WaitForSelectorAsync("[data-testid='stat-cards']", new() { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='status-detail-tabs']", new() { Timeout = 10_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "status-detail");
        });

    // ── Status Detail: Resend dialog ─────────────────────────────────────────

    [Fact]
    public Task StatusDetailPage_ResendDialog_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/status/1");
            await page.WaitForSelectorAsync("[data-testid='detail-actions']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Open resend dialog
            await page.ClickAsync("[data-testid='action-resend']");
            await page.WaitForSelectorAsync("[data-testid='resend-dialog']", new() { Timeout = 8_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "status-detail-resend-dialog");

            // Close dialog via Escape
            await page.Keyboard.PressAsync("Escape");
        });

    // ── Send Wizard: Step 1 (method selection) ───────────────────────────────

    [Fact]
    public Task WizardPage_Step1_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/send/wizard");
            await page.WaitForSelectorAsync("[data-testid='wizard-step-indicator']", new() { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='wizard-method-grid']", new() { Timeout = 10_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "wizard-step1-method");
        });

    // ── Send Wizard: Step 2 (recipients after selecting SMS) ─────────────────

    [Fact]
    public Task WizardPage_Step2_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/send/wizard");
            await page.WaitForSelectorAsync("[data-testid='wizard-method-grid']", new() { Timeout = 15_000 });

            // Select the first method card (by-address / address-based)
            await page.ClickAsync("[data-testid='method-card-by-address']");

            // Wait for the Next button to become enabled, then click it
            var nextBtn = page.Locator("[data-testid='wizard-next']");
            await nextBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
            await nextBtn.ClickAsync();

            // Wait for step 2 heading
            await page.WaitForSelectorAsync("[data-testid='wizard-heading']", new() { Timeout = 8_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "wizard-step2-recipients");
        });

    // ── Select Customer ──────────────────────────────────────────────────────

    [Fact]
    public Task SelectCustomerPage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/select-customer");
            await page.WaitForSelectorAsync("[data-testid='select-customer-page']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "select-customer");
        });

    // ── Select Profile ───────────────────────────────────────────────────────

    [Fact]
    public Task SelectProfilePage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/select-profile");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            // Page may redirect if only one profile — capture regardless
            await page.WaitForTimeoutAsync(1_500);

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "select-profile");
        });

    // ── Customer Admin (tabs) ─────────────────────────────────────────────────

    [Fact]
    public Task CustomerAdminPage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/customer-admin");
            await page.WaitForSelectorAsync("[data-testid='customer-admin-heading']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "customer-admin-tabs");
        });

    // ── Admin User List ───────────────────────────────────────────────────────

    [Fact]
    public Task AdminUserListPage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/admin/users");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1_000);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "admin-user-list");
        });

    // ── Admin Settings ────────────────────────────────────────────────────────

    [Fact]
    public Task AdminSettingsPage_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.GotoAsync($"{BaseUrl}/admin/settings");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForTimeoutAsync(1_000);

            await AssertNoHorizontalOverflowAsync(page, device);
            await AssertTopBarNotClippingContentAsync(page, device);
            await AssertNoTextOverflowAsync(page, device);
            await CaptureAsync(page, device, "admin-settings");
        });

    // ── BroadcastingHub: QuickSend tabs (SMS vs Email) ───────────────────────

    [Fact]
    public Task BroadcastingHub_QuickSendEmailTab_AllDevices()
        => ForEachDeviceAsync(async (page, device) =>
        {
            await LoginAsync(page);
            await page.WaitForSelectorAsync("[data-testid='quick-send-tabs']", new() { Timeout = 15_000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Click the Email tab (second tab in quick-send-tabs, index 1)
            await page.Locator("[data-testid='quick-send-tabs'] .mud-tab").Nth(1).ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='quick-send-email']", new() { Timeout = 5_000 });

            await AssertNoHorizontalOverflowAsync(page, device);
            await CaptureAsync(page, device, "broadcasting-hub-email-tab");
        });
}
