using Microsoft.Playwright;

namespace GreenAi.E2E;

[Collection("E2E")]
public sealed class CustomerAdminE2ETests : E2ETestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToHome()
    {
        await LoginAsync();
        Assert.DoesNotContain("/login", Page.Url);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[data-testid='blazor-circuit-ready']",
            new PageWaitForSelectorOptions { Timeout = 20_000, State = WaitForSelectorState.Attached });

        await Page.ClickAsync("input[type='email']");
        await Page.FillAsync("input[type='email']", "claus.elmann@gmail.com");
        await Page.PressAsync("input[type='email']", "Tab");
        await Page.FillAsync("input[type='password']", "forkert");
        await Page.PressAsync("input[type='password']", "Enter");

        await WaitOrFailAsync(".mud-alert-message", timeoutMs: 8_000, hint: "Error alert should appear after wrong password");
        Assert.Contains("/login", Page.Url);
    }

    [Fact]
    public async Task CustomerAdmin_AfterLogin_ShowsHeading()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");

        // Blazor Server: data loads in OnAfterRenderAsync — give circuit time to establish
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000,
            hint: "Heading should be visible after login. If redirected, check BlazorPrincipalHolder + ICurrentUser DI.");

        var heading = Page.Locator("[data-testid='page-title']");
        Assert.Equal("Kundestyre", await heading.InnerTextAsync());

        await AssertNoVisibleErrorsAsync("CustomerAdmin page");
        await AssertNoMissingLabelsAsync("CustomerAdmin page");
    }

    [Fact]
    public async Task CustomerAdmin_UsersTab_ShowsSeedUsers()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");

        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Brugere");
        await WaitOrFailAsync("[data-testid='user-table']", timeoutMs: 10_000);

        var content = await Page.Locator("[data-testid='user-table']").InnerTextAsync();
        Assert.Contains("claus.elmann@gmail.com", content);
        Assert.Contains("sender@dev.local", content);
    }

    [Fact]
    public async Task CustomerAdmin_SettingsTab_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await AssertNoVisibleErrorsAsync("CustomerAdmin / Settings tab");
        await AssertNoMissingLabelsAsync("CustomerAdmin / Settings tab");
    }

    [Fact]
    public async Task CustomerAdmin_UsersTab_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Brugere");
        await WaitOrFailAsync("[data-testid='user-table']", timeoutMs: 10_000);
        await AssertNoVisibleErrorsAsync("CustomerAdmin / Users tab");
        await AssertNoMissingLabelsAsync("CustomerAdmin / Users tab");
    }

    [Fact]
    public async Task CustomerAdmin_ProfilesTab_NoMissingLabels()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");
        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Profiler");
        await WaitOrFailAsync("[data-testid='profile-table']", timeoutMs: 10_000);
        await AssertNoVisibleErrorsAsync("CustomerAdmin / Profiles tab");
        await AssertNoMissingLabelsAsync("CustomerAdmin / Profiles tab");
    }

    [Fact]
    public async Task CustomerAdmin_ProfilesTab_ShowsSeedProfiles()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BaseUrl}/customer-admin");

        await WaitOrFailAsync("[data-testid='customer-admin-heading']", timeoutMs: 20_000);
        await Page.ClickAsync("text=Profiler");
        await WaitOrFailAsync("[data-testid='profile-table']", timeoutMs: 10_000);

        var content = await Page.Locator("[data-testid='profile-table']").InnerTextAsync();
        Assert.Contains("Nordjylland", content);
        Assert.Contains("Sønderjylland", content);
    }

    [Fact]
    public async Task CustomerAdmin_UnauthenticatedAccess_RedirectsToLogin()
    {
        await Page.GotoAsync($"{BaseUrl}/customer-admin");

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline && !Page.Url.Contains("/login"))
            await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Contains("/login", Page.Url);
    }
}

