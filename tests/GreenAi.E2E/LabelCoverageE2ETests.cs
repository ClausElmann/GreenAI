namespace GreenAi.E2E;

/// <summary>
/// Label coverage smoke tests for pages not covered by CustomerAdminE2ETests or DetailPageE2ETests.
///
/// Pages covered here:
///   /user/profile          — UserProfilePage (feature.userProfile.*)
///   /admin/users           — AdminUserListPage (feature.adminUsers.*)
///   /admin/settings        — AdminSettingsPage (feature.adminSettings.*)
/// </summary>
[Collection("E2E")]
public sealed class LabelCoverageE2ETests : E2ETestBase
{
    [Fact]
    public async Task UserProfile_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/user/profile");
        await WaitOrFailAsync("[data-testid='save-profile-button']", timeoutMs: 15_000);
        await AssertNoMissingLabelsAsync("UserProfile page");
    }

    [Fact]
    public async Task AdminUsers_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/users");
        await WaitOrFailAsync("[data-testid='create-user-button']", timeoutMs: 15_000);
        await AssertNoMissingLabelsAsync("AdminUserList page");
    }

    [Fact]
    public async Task AdminSettings_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/admin/settings");
        await WaitOrFailAsync("[data-testid='settings-table']", timeoutMs: 15_000);
        await AssertNoMissingLabelsAsync("AdminSettings page");
    }
}
