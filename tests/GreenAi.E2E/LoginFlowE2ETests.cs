using Microsoft.Data.SqlClient;
using Microsoft.Playwright;

namespace GreenAi.E2E;

/// <summary>
/// E2E tests for the login resolution flow.
///
/// Three scenarios (all use isolated test accounts, not the primary dev account):
///   1. Single customer + single profile → direct to /broadcasting (no selection pages)
///   2. Multiple customers → /select-customer page → select one → /broadcasting
///   3. Single customer + multiple profiles → /select-profile page → select one → /broadcasting
///
/// Each test creates its own isolated DB state (user + memberships + profiles) and
/// cleans up in DisposeAsync. This avoids conflicts with the primary E2E seed data.
/// </summary>
[Collection("E2E")]
public sealed class LoginFlowE2ETests : E2ETestBase
{
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Integrated Security=true;TrustServerCertificate=true;";

    // Isolated test accounts — not used by any other test or the shared fixture
    private const string SingleCtxEmail    = "e2e.single@loginflow.test";
    private const string MultiCustomerEmail = "e2e.multicustomer@loginflow.test";
    private const string MultiProfileEmail  = "e2e.multiprofile@loginflow.test";

    // Use the same password + hash/salt as the primary E2E account (pre-computed for "Flipper12#").
    // This avoids a runtime dependency on PasswordHasher from the E2E project.
    private const string TestPassword   = "Flipper12#";
    private const string TestPwdHash    = "N9p1t00iogUQwhDCgeFGRQgv174X9Wjc+NKjIg7g7LdHVGGBtrK88r5jwRsfM7bQszVQV9+333ASHfJ8qKjAhg==";
    private const string TestPwdSalt    = "VlN9lBRMfoASx0x6+OrUpbA0TTHXi/X8cEpXU2mYauk=";

    // IDs resolved during seed — kept for cleanup
    private readonly List<(string Sql, object? Params)> _cleanupSqls = [];

    // -------------------------------------------------------------------------
    // Scenario 1: 1 customer + 1 profile → direct to /broadcasting
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_SingleCustomerSingleProfile_NavigatesDirectlyToBroadcasting()
    {
        var (userId, customerId, _) = await SeedSingleContextUserAsync(SingleCtxEmail);

        try
        {
            await LoginViaUiAsync(SingleCtxEmail, TestPassword);
            await WaitOrFailAsync("[data-testid='send-methods-grid']", timeoutMs: 20_000,
                hint: "Expected direct navigation to /broadcasting after login with exactly 1 customer + 1 profile");

            Assert.Contains("/broadcasting", Page.Url);
            await AssertNoVisibleErrorsAsync("broadcasting hub after single-context login");
        }
        finally
        {
            await CleanupUserAsync(userId, customerId);
        }
    }

    // -------------------------------------------------------------------------
    // Scenario 2: 2 customers → /select-customer → pick one → /broadcasting
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_MultipleCustomers_ShowsSelectCustomerPage()
    {
        var (userId, customerA, customerB) = await SeedMultiCustomerUserAsync(MultiCustomerEmail);

        try
        {
            await LoginViaUiAsync(MultiCustomerEmail, TestPassword);
            await WaitOrFailAsync("[data-testid='select-customer-page']", timeoutMs: 20_000,
                hint: "Expected /select-customer page when user has 2 customer memberships");

            Assert.Contains("/select-customer", Page.Url);
            await AssertNoVisibleErrorsAsync("select-customer page");

            // Customer cards should be present
            await WaitOrFailAsync("[data-testid='customer-list']",
                hint: "Customer list must be rendered on /select-customer");
        }
        finally
        {
            await CleanupUserAsync(userId, customerA, customerB);
        }
    }

    [Fact]
    public async Task Login_MultipleCustomers_SelectCustomer_NavigatesToBroadcasting()
    {
        var (userId, customerA, customerB) = await SeedMultiCustomerUserAsync(MultiCustomerEmail);

        try
        {
            await LoginViaUiAsync(MultiCustomerEmail, TestPassword);
            await WaitOrFailAsync("[data-testid='select-customer-page']", timeoutMs: 20_000,
                hint: "Expected /select-customer page");

            // Click the first customer card (customerA has exactly 1 profile → auto-resolves JWT)
            await Page.ClickAsync($"[data-testid='customer-card-{customerA}']");

            await WaitOrFailAsync("[data-testid='send-methods-grid']", timeoutMs: 20_000,
                hint: "Expected navigation to /broadcasting after selecting a customer with 1 profile");

            Assert.Contains("/broadcasting", Page.Url);
            await AssertNoVisibleErrorsAsync("broadcasting hub after customer selection");
        }
        finally
        {
            await CleanupUserAsync(userId, customerA, customerB);
        }
    }

    // -------------------------------------------------------------------------
    // Scenario 3: 1 customer + 2 profiles → /select-profile → pick one → /broadcasting
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_SingleCustomerMultipleProfiles_ShowsSelectProfilePage()
    {
        var (userId, customerId, _, _) = await SeedMultiProfileUserAsync(MultiProfileEmail);

        try
        {
            await LoginViaUiAsync(MultiProfileEmail, TestPassword);
            await WaitOrFailAsync("[data-testid='select-profile-page']", timeoutMs: 20_000,
                hint: "Expected /select-profile page when user has 1 customer + 2 profiles");

            Assert.Contains("/select-profile", Page.Url);
            await AssertNoVisibleErrorsAsync("select-profile page");

            // Profile cards should be present
            await WaitOrFailAsync("[data-testid='profile-list']",
                hint: "Profile list must be rendered on /select-profile");
        }
        finally
        {
            await CleanupUserAsync(userId, customerId);
        }
    }

    [Fact]
    public async Task Login_SingleCustomerMultipleProfiles_SelectProfile_NavigatesToBroadcasting()
    {
        var (userId, customerId, profileA, _) = await SeedMultiProfileUserAsync(MultiProfileEmail);

        try
        {
            await LoginViaUiAsync(MultiProfileEmail, TestPassword);
            await WaitOrFailAsync("[data-testid='select-profile-page']", timeoutMs: 20_000,
                hint: "Expected /select-profile page");

            // Click the first profile card
            await Page.ClickAsync($"[data-testid='profile-card-{profileA}']");

            await WaitOrFailAsync("[data-testid='send-methods-grid']", timeoutMs: 20_000,
                hint: "Expected navigation to /broadcasting after selecting a profile");

            Assert.Contains("/broadcasting", Page.Url);
            await AssertNoVisibleErrorsAsync("broadcasting hub after profile selection");
        }
        finally
        {
            await CleanupUserAsync(userId, customerId);
        }
    }

    // =========================================================================
    // UI helpers
    // =========================================================================

    /// <summary>
    /// Drives the login form directly — no token injection.
    /// Waits for the Blazor circuit before submitting so event handlers are wired.
    /// </summary>
    private async Task LoginViaUiAsync(string email, string password)
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.WaitForSelectorAsync("[data-testid='blazor-circuit-ready']",
            new PageWaitForSelectorOptions { Timeout = 20_000, State = WaitForSelectorState.Attached });

        // MudBlazor MudTextField renders a wrapper div — use input type selectors (same as CustomerAdminE2ETests)
        await Page.FillAsync("input[type='email']", email);
        await Page.FillAsync("input[type='password']", password);
        await Page.ClickAsync("[data-testid='login-submit']");
    }

    // =========================================================================
    // DB seed / cleanup helpers
    // =========================================================================

    /// <summary>
    /// 1 customer + 1 profile → login auto-resolves full JWT → /broadcasting
    /// Returns (userId, customerId, profileId).
    /// </summary>
    private async Task<(int UserId, int CustomerId, int ProfileId)> SeedSingleContextUserAsync(string email)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var userId = await UpsertUserAsync(conn, email);
        var customerId = await UpsertCustomerAsync(conn, $"E2E Single ({email})");
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {userId} AND [CustomerId] = {customerId})
                INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId]) VALUES ({userId}, {customerId}, 1);
            """);
        var profileId = await UpsertProfileAsync(conn, customerId, "Primary Profile");
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profileId} AND [UserId] = {userId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profileId}, {userId});
            """);

        return (userId, customerId, profileId);
    }

    /// <summary>
    /// 2 customers, each with 1 profile → login returns NeedsCustomerSelection → /select-customer
    /// Returns (userId, customerAId, customerBId).
    /// </summary>
    private async Task<(int UserId, int CustomerA, int CustomerB)> SeedMultiCustomerUserAsync(string email)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var userId    = await UpsertUserAsync(conn, email);
        var customerA = await UpsertCustomerAsync(conn, $"E2E CustA ({email})");
        var customerB = await UpsertCustomerAsync(conn, $"E2E CustB ({email})");

        // Memberships for both customers
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {userId} AND [CustomerId] = {customerA})
                INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId]) VALUES ({userId}, {customerA}, 1);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {userId} AND [CustomerId] = {customerB})
                INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId]) VALUES ({userId}, {customerB}, 1);
            """);

        // Each customer has 1 profile for the user (so selecting a customer auto-resolves JWT)
        var profileA = await UpsertProfileAsync(conn, customerA, "Profile A");
        var profileB = await UpsertProfileAsync(conn, customerB, "Profile B");
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profileA} AND [UserId] = {userId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profileA}, {userId});
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profileB} AND [UserId] = {userId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profileB}, {userId});
            """);

        return (userId, customerA, customerB);
    }

    /// <summary>
    /// 1 customer + 2 profiles → login returns NeedsProfileSelection → /select-profile
    /// Returns (userId, customerId, profileAId, profileBId).
    /// </summary>
    private async Task<(int UserId, int CustomerId, int ProfileA, int ProfileB)> SeedMultiProfileUserAsync(string email)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var userId     = await UpsertUserAsync(conn, email);
        var customerId = await UpsertCustomerAsync(conn, $"E2E MultiProfile ({email})");

        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {userId} AND [CustomerId] = {customerId})
                INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId]) VALUES ({userId}, {customerId}, 1);
            """);

        var profileA = await UpsertProfileAsync(conn, customerId, "Profile Alpha");
        var profileB = await UpsertProfileAsync(conn, customerId, "Profile Beta");
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profileA} AND [UserId] = {userId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profileA}, {userId});
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profileB} AND [UserId] = {userId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profileB}, {userId});
            """);

        return (userId, customerId, profileA, profileB);
    }

    /// <summary>
    /// Removes all data created by seed helpers for the given userId.
    /// Removes memberships, profile mappings, profiles, customers, and the user.
    /// customerIds: pass any number of customer IDs to clean up their profiles + customers.
    /// </summary>
    private static async Task CleanupUserAsync(int userId, params int[] customerIds)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Refresh tokens + role mappings for user
        await ExecAsync(conn, $"DELETE FROM [dbo].[UserRefreshTokens] WHERE [UserId] = {userId}");
        await ExecAsync(conn, $"DELETE FROM [dbo].[UserRoleMappings] WHERE [UserId] = {userId}");

        foreach (var cid in customerIds)
        {
            // Profile cleanup (mappings → profiles)
            await ExecAsync(conn, $"""
                DELETE pum FROM [dbo].[ProfileUserMappings] pum
                    JOIN [dbo].[Profiles] p ON p.[Id] = pum.[ProfileId]
                    WHERE p.[CustomerId] = {cid} AND pum.[UserId] = {userId};
                DELETE FROM [dbo].[Profiles] WHERE [CustomerId] = {cid};
                DELETE FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {userId} AND [CustomerId] = {cid};
                DELETE FROM [dbo].[Customers] WHERE [Id] = {cid};
                """);
        }

        await ExecAsync(conn, $"DELETE FROM [dbo].[Users] WHERE [Id] = {userId}");
    }

    private static async Task<int> UpsertUserAsync(SqlConnection conn, string email)
    {
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = '{EscapeSql(email)}')
                INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [PasswordSalt], [IsActive])
                VALUES (
                    '{EscapeSql(email)}',
                    '{TestPwdHash}',
                    '{TestPwdSalt}',
                    1);
            """);

        return await ScalarAsync<int>(conn, $"SELECT [Id] FROM [dbo].[Users] WHERE [Email] = '{EscapeSql(email)}'");
    }

    private static async Task<int> UpsertCustomerAsync(SqlConnection conn, string name)
    {
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Name] = '{EscapeSql(name)}')
                INSERT INTO [dbo].[Customers] ([Name]) VALUES ('{EscapeSql(name)}');
            """);

        return await ScalarAsync<int>(conn, $"SELECT [Id] FROM [dbo].[Customers] WHERE [Name] = '{EscapeSql(name)}'");
    }

    private static async Task<int> UpsertProfileAsync(SqlConnection conn, int customerId, string displayName)
    {
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Profiles] WHERE [CustomerId] = {customerId} AND [DisplayName] = '{EscapeSql(displayName)}')
                INSERT INTO [dbo].[Profiles] ([CustomerId], [DisplayName]) VALUES ({customerId}, '{EscapeSql(displayName)}');
            """);

        return await ScalarAsync<int>(conn, $"SELECT [Id] FROM [dbo].[Profiles] WHERE [CustomerId] = {customerId} AND [DisplayName] = '{EscapeSql(displayName)}'");
    }

    private static string EscapeSql(string value) => value.Replace("'", "''");

    private static async Task ExecAsync(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }
}
