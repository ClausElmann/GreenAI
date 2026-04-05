namespace GreenAi.E2E;

/// <summary>
/// Label coverage smoke tests for pages not covered by CustomerAdminE2ETests or DetailPageE2ETests.
///
/// Pages covered here:
///   /dashboard             — deprecated redirect → /broadcasting
///   /broadcasting          — BroadcastingHubPage (page.broadcasting.*)
///   /send/wizard           — SendWizardPage (nav.Send, page.wizard.*)
///   /status                — StatusPage (page.statusList.*) — tabs: Sent/Scheduled/Failed
///   /status/1              — StatusDetailPage (page.statusDetail.*)
///   /admin/super           — SuperAdminPage (page.superAdmin.*)
///   /drafts                — DraftsPage (nav.Drafts, page.drafts.*)
///   /user/profile          — UserProfilePage (feature.userProfile.*)
///   /admin/users           — AdminUserListPage (feature.adminUsers.*)
///   /admin/settings        — AdminSettingsPage (feature.adminSettings.*)
///   /select-customer       — SelectCustomerPage (page.selectCustomer.*)
///   /select-profile        — SelectProfilePage (page.selectProfile.*)
/// </summary>
[Collection("E2E")]
public sealed class LabelCoverageE2ETests : E2ETestBase
{
    [Fact]
    public async Task Dashboard_RedirectsToBroadcasting()
    {
        // /dashboard is deprecated — verifies it redirects to /broadcasting correctly
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/dashboard");
        await WaitOrFailAsync("[data-testid='send-methods-grid']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("Dashboard redirect → Broadcasting");
        await AssertNoMissingLabelsAsync("Dashboard redirect → Broadcasting");
    }

    [Fact]
    public async Task SendWizard_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/send/wizard");
        await WaitOrFailAsync("[data-testid='wizard-step-indicator']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("SendWizard page");
        await AssertNoMissingLabelsAsync("SendWizard page");
    }

    [Fact]
    public async Task Status_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/status");
        await WaitOrFailAsync("[data-testid='status-list-table']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("Status page");
        await AssertNoMissingLabelsAsync("Status page");
    }

    [Fact]
    public async Task StatusDetail_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/status/1");
        await WaitOrFailAsync("[data-testid='stat-cards']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("StatusDetail page");
        await AssertNoMissingLabelsAsync("StatusDetail page");
    }

    [Fact]
    public async Task Broadcasting_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/broadcasting");
        await WaitOrFailAsync("[data-testid='send-methods-grid']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("Broadcasting hub page");
        await AssertNoMissingLabelsAsync("Broadcasting hub page");
    }

    [Fact]
    public async Task SuperAdmin_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/super");
        await WaitOrFailAsync("[data-testid='super-admin-context-selector']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("SuperAdmin page");
        await AssertNoMissingLabelsAsync("SuperAdmin page");
    }

    [Fact]
    public async Task Drafts_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/drafts");
        await WaitOrFailAsync("[data-testid='drafts-table']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("Drafts page");
        await AssertNoMissingLabelsAsync("Drafts page");
    }

    [Fact]
    public async Task UserProfile_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/user/profile");
        await WaitOrFailAsync("[data-testid='save-profile-button']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("UserProfile page");
        await AssertNoMissingLabelsAsync("UserProfile page");
    }

    [Fact]
    public async Task AdminUsers_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await WaitOrFailAsync("[data-testid='create-user-button']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("AdminUserList page");
        await AssertNoMissingLabelsAsync("AdminUserList page");
    }

    [Fact]
    public async Task AdminSettings_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/settings");
        await WaitOrFailAsync("[data-testid='settings-table']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("AdminSettings page");
        await AssertNoMissingLabelsAsync("AdminSettings page");
    }

    [Fact]
    public async Task SelectCustomer_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/select-customer");
        // Wait for page container (cards are empty when accessed directly — card labels covered by LoginFlowE2ETests)
        await WaitOrFailAsync("[data-testid='select-customer-page']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("SelectCustomer page");
        await AssertNoMissingLabelsAsync("SelectCustomer page");
    }

    [Fact]
    public async Task SelectProfile_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/select-profile");
        // Wait for page container (cards are empty when accessed directly — card labels covered by LoginFlowE2ETests)
        await WaitOrFailAsync("[data-testid='select-profile-page']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("SelectProfile page");
        await AssertNoMissingLabelsAsync("SelectProfile page");
    }
}
