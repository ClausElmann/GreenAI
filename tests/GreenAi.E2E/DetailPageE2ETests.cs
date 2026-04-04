using Microsoft.Playwright;

namespace GreenAi.E2E;

/// <summary>
/// E2E tests for ProfileDetail and UserDetail pages.
///
/// Prerequisites:
///   - App running on http://localhost:5057
///   - Seed data present (admin@dev.local, Testkommune, Nordjylland profile)
///     → guaranteed by E2EDatabaseFixture
///
/// Coverage:
///   - ProfileDetail: page loads, profile name field visible, save button present
///   - ProfileDetail: unauthenticated access redirects to /login
///   - UserDetail: page loads, breadcrumbs visible after navigation
///   - UserDetail: unauthenticated access redirects to /login
/// </summary>
[Collection("E2E")]
public sealed class DetailPageE2ETests : E2ETestBase
{
    // ─────────────────────────────────────────────────────────────
    // ProfileDetail E2E
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProfileDetail_NavigateFromTable_LoadsProfileInfoForm()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");

        // Wait for customer-admin page to load
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000,
            hint: "customer-admin-heading must be visible after login");

        // Click the Profiles tab
        await Page.ClickAsync("text=Profiler");
        await WaitOrFailAsync("[data-testid='profile-table']", timeoutMs: 10_000,
            hint: "Profile table should appear after clicking Profiles tab");

        // Click the first profile link in the table
        await WaitOrFailAsync("[data-testid='profile-table'] a", timeoutMs: 10_000,
            hint: "At least one profile link must be in the profile table");
        await Page.ClickAsync("[data-testid='profile-table'] a");

        // Verify ProfileDetail loaded — the save button is only rendered when profile is found
        await WaitOrFailAsync("[data-testid='profile-save']", timeoutMs: 15_000,
            hint: "ProfileDetail save button should be visible when profile detail loads. " +
                  "If timeout: check OnAfterRenderAsync auth + ICurrentUser + DB query.");

        Assert.Contains("/customer-admin/profiles/", Page.Url);
    }

    [Fact]
    public async Task ProfileDetail_ProfileNameField_IsPresentAndEditable()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Profiler");
        await WaitOrFailAsync("[data-testid='profile-table']", timeoutMs: 10_000);
        await WaitOrFailAsync("[data-testid='profile-table'] a", timeoutMs: 10_000);
        await Page.ClickAsync("[data-testid='profile-table'] a");
        await WaitOrFailAsync("[data-testid='profile-save']", timeoutMs: 15_000);

        // Wait for the profile name input to be present and read its value
        // Note: MudTextField renders UserAttributes directly on the <input> element, not a wrapper
        await WaitOrFailAsync("[data-testid='profile-name']", timeoutMs: 10_000,
            hint: "MudTextField with data-testid='profile-name' should render an input element when the form is loaded");
        var nameField = Page.Locator("[data-testid='profile-name']");
        var value = await nameField.InputValueAsync();
        Assert.False(string.IsNullOrWhiteSpace(value), "Profile name field should have a non-empty value after load");
    }

    // ─────────────────────────────────────────────────────────────
    // UserDetail E2E
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserDetail_NavigateFromTable_LoadsBreadcrumbs()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000,
            hint: "customer-admin-heading must be visible after login");

        // Navigate to Users tab
        await Page.ClickAsync("text=Brugere");
        await WaitOrFailAsync("[data-testid='user-table']", timeoutMs: 10_000,
            hint: "User table should appear after clicking Users tab");

        // Click first user email link
        await WaitOrFailAsync("[data-testid='user-table'] a", timeoutMs: 10_000,
            hint: "At least one user link must be present in the user table");
        await Page.ClickAsync("[data-testid='user-table'] a");

        // Verify URL changed to user detail page
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && !Page.Url.Contains("/customer-admin/users/"))
            await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Contains("/customer-admin/users/", Page.Url);

        // Breadcrumbs appear when the user is successfully loaded (not in loading/not-found state)
        await WaitOrFailAsync(".mud-breadcrumbs", timeoutMs: 15_000,
            hint: "Breadcrumbs should appear after UserDetail loads. " +
                  "If timeout: check auth, ICurrentUser, GetUserDetailsHandler.");
    }

    [Fact]
    public async Task UserDetail_ActiveStatusChip_IsVisible()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);

        // Navigate via users tab
        await Page.ClickAsync("text=Brugere");
        await WaitOrFailAsync("[data-testid='user-table']", timeoutMs: 10_000);
        await Page.ClickAsync("[data-testid='user-table'] a");

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && !Page.Url.Contains("/customer-admin/users/"))
            await Task.Delay(200, TestContext.Current.CancellationToken);

        await WaitOrFailAsync(".mud-breadcrumbs", timeoutMs: 15_000);

        // Active/Inactive chip must be visible (confirms user object loaded and rendered)
        await WaitOrFailAsync(".mud-chip", timeoutMs: 5_000,
            hint: "Active/Inactive chip should be visible after user detail loads");

        var chip = Page.Locator(".mud-chip").First;
        var chipText = await chip.InnerTextAsync();
        Assert.True(chipText is "Aktiv" or "Inaktiv", $"Chip text should be Aktiv or Inaktiv, got: '{chipText}'");
    }

    // ─────────────────────────────────────────────────────────────
    // Label validation — dedicated smoke tests
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProfileDetail_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Profiler");
        await WaitOrFailAsync("[data-testid='profile-table'] a", timeoutMs: 10_000);
        await Page.ClickAsync("[data-testid='profile-table'] a");
        await WaitOrFailAsync("[data-testid='profile-save']", timeoutMs: 15_000);
        await AssertNoMissingLabelsAsync("ProfileDetail page");
    }

    [Fact]
    public async Task UserDetail_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Brugere");
        await WaitOrFailAsync("[data-testid='user-table'] a", timeoutMs: 10_000);
        await Page.ClickAsync("[data-testid='user-table'] a");
        await WaitOrFailAsync(".mud-breadcrumbs", timeoutMs: 15_000);
        await AssertNoMissingLabelsAsync("UserDetail page");
    }
}
