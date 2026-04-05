namespace GreenAi.E2E;

/// <summary>
/// Label coverage smoke tests for pages not covered by CustomerAdminE2ETests or DetailPageE2ETests.
///
/// Pages covered here:
///   /dashboard             — DashboardPage (dashboard.*, nav.*, shared.*)
///   /send/wizard           — SendWizardPage (nav.Send, page.wizard.*)
///   /status                — StatusPage (nav.Status, page.status.*)
///   /drafts                — DraftsPage (nav.Drafts, page.drafts.*)
///   /user/profile          — UserProfilePage (feature.userProfile.*)
///   /admin/users           — AdminUserListPage (feature.adminUsers.*)
///   /admin/settings        — AdminSettingsPage (feature.adminSettings.*)
/// </summary>
[Collection("E2E")]
public sealed class LabelCoverageE2ETests : E2ETestBase
{
    [Fact]
    public async Task Dashboard_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/dashboard");
        await WaitOrFailAsync("[data-testid='dashboard-placeholder']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("Dashboard page");
        await AssertNoMissingLabelsAsync("Dashboard page");
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
        await WaitOrFailAsync("[data-testid='status-page-placeholder']", timeoutMs: 15_000);
        await AssertNoVisibleErrorsAsync("Status page");
        await AssertNoMissingLabelsAsync("Status page");
    }

    [Fact]
    public async Task Drafts_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/drafts");
        await WaitOrFailAsync("[data-testid='drafts-page-placeholder']", timeoutMs: 15_000);
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
}
