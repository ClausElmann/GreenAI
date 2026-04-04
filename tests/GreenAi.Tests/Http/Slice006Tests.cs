using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Http;

/// <summary>
/// SLICE-006 — Golden sample (HARD GATE).
///
/// Proves all 6 foundation domains work end-to-end:
///
/// Domain coverage:
///   1. Auth          — real HTTP login returns access token (LoginHandler + JWT)
///   2. Tenant        — single membership → auto-assign customer + profile (ProfileResolutionResult)
///   3. Middleware    — C_001: profileId=0 → hard 401. C_005: customerId=0 → hard 401
///   4. Labels        — GET /api/localization/{languageId} returns dictionary (>0 entries)
///   5. Permissions   — DoesProfileHaveRoleAsync returns true after role assignment
///   6. Identity      — GET /api/auth/me returns correct UserId/CustomerId/ProfileId
///
/// Gate rule: ALL tests in this file must pass before feature domains may begin.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class Slice006Tests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;
    private readonly AuthTestDataBuilder _builder;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Slice006Tests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db     = db;
        _client = factory.CreateClient();
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        // Clean up test labels
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("DELETE FROM [dbo].[Labels] WHERE [ResourceName] LIKE 'test.%'");
    }

    // =========================================================================
    // 1 + 2 + 6 — Auth + Tenant + Identity: real login → auto-assign → /me
    // =========================================================================

    [Fact]
    public async Task GoldenPath_Login_AutoAssign_Me()
    {
        // Arrange — 1 customer + 1 profile → guarantees auto-assign in LoginHandler
        var (hash, salt) = PasswordHasher.Hash("golden-pass-123");
        var customerId   = await _builder.InsertCustomerAsync("Golden Customer");
        var userId       = await _builder.InsertUserAsync(new()
        {
            Email        = "golden@test.local",
            PasswordHash = hash,
            PasswordSalt = salt
        });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId, languageId: 1);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Golden Profile");

        // Act — POST /api/auth/login
        var loginResponse = await PostJsonAsync("/api/auth/login", new
        {
            Email    = "golden@test.local",
            Password = "golden-pass-123"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var loginDoc  = JsonDocument.Parse(loginBody);

        // Auto-assign: should have access token directly (NeedsCustomerSelection = false)
        Assert.False(loginDoc.RootElement.TryGetProperty("needsCustomerSelection", out var ncs) && ncs.GetBoolean(),
            "Expected auto-assign (1 customer) but got NeedsCustomerSelection=true");

        var accessToken = loginDoc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken), "Expected non-empty access token");

        // Act — GET /api/auth/me with the token
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var meResponse = await _client.SendAsync(meRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meBody = await meResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var meDoc  = JsonDocument.Parse(meBody);

        // Assert identity fields from JWT (auto-assigned customer + profile)
        Assert.Equal(userId.Value,     meDoc.RootElement.GetProperty("userId").GetInt32());
        Assert.Equal(customerId.Value, meDoc.RootElement.GetProperty("customerId").GetInt32());
        Assert.Equal(profileId,        meDoc.RootElement.GetProperty("profileId").GetInt32());
        Assert.Equal("golden@test.local", meDoc.RootElement.GetProperty("email").GetString());
    }

    // =========================================================================
    // 3 — Security: C_001 (profileId=0 → hard 401) + C_005 (customerId=0 → hard 401)
    // =========================================================================

    [Fact]
    public async Task C001_ProfileId0_ProtectedRoute_Returns401()
    {
        // Token with valid customerId but profileId=0 — CurrentUserMiddleware must block
        var token = TestJwtHelper.CreateToken(new UserId(1), new CustomerId(99), new ProfileId(0));
        var response = await GetWithToken("/api/health", token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task C005_CustomerId0_ProtectedRoute_Returns401()
    {
        // Token with valid profileId but customerId=0 — CurrentUserMiddleware must block
        var token = TestJwtHelper.CreateToken(new UserId(1), new CustomerId(0), new ProfileId(99));
        var response = await GetWithToken("/api/health", token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task C001_C005_AuthRoutes_ExemptFromMiddleware()
    {
        // /api/auth/* must NOT be blocked even with zero IDs (login + refresh must work before profile selection)
        var token = TestJwtHelper.CreateToken(new UserId(1), new CustomerId(0), new ProfileId(0));
        var response = await GetWithToken("/api/auth/me", token);
        // Should reach the handler (returns 200 with the claims — even if zeros)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // =========================================================================
    // 4 — Labels: GET /api/localization/{languageId} returns dictionary
    // =========================================================================

    [Fact]
    public async Task Labels_GetDictionary_ReturnsEntries()
    {
        // Insert a test label for DA (languageId=1)
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Labels] WHERE [LanguageId]=1 AND [ResourceName]='test.golden')
                INSERT INTO [dbo].[Labels] ([ResourceName],[ResourceValue],[LanguageId]) VALUES ('test.golden','Guld',1)
            """);

        var response = await _client.GetAsync("/api/localization/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body   = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(body, JsonOptions);

        Assert.NotNull(labels);
        Assert.True(labels.ContainsKey("test.golden"), "Expected 'test.golden' label in response");
        Assert.Equal("Guld", labels["test.golden"]);
    }

    // =========================================================================
    // 5 — Permissions: DoesProfileHaveRoleAsync via PermissionService (SQL)
    // =========================================================================

    [Fact]
    public async Task Permissions_ProfileWithRole_ReturnsTrue()
    {
        // Arrange — profile + assign a known ProfileRole
        var customerId = await _builder.InsertCustomerAsync("Perm Customer");
        var userId     = await _builder.InsertUserAsync(new() { Email = "perm@test.local" });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId);
        await _builder.AssignProfileRoleAsync(profileId, "CanSendByEboks");

        // Act — use PermissionService directly via the test DB session
        var service = new GreenAi.Api.SharedKernel.Permissions.PermissionService(
            new GreenAi.Api.SharedKernel.Db.DbSession(DatabaseFixture.ConnectionString));

        var canView = await service.DoesProfileHaveRoleAsync(
            new ProfileId(profileId),
            "CanSendByEboks");

        Assert.True(canView, "Expected DoesProfileHaveRoleAsync=true after role assignment");
    }

    [Fact]
    public async Task Permissions_ProfileWithoutRole_ReturnsFalse()
    {
        var customerId = await _builder.InsertCustomerAsync("NoPerm Customer");
        var userId     = await _builder.InsertUserAsync(new() { Email = "noperm@test.local" });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId);
        // No role assigned

        var service = new GreenAi.Api.SharedKernel.Permissions.PermissionService(
            new GreenAi.Api.SharedKernel.Db.DbSession(DatabaseFixture.ConnectionString));

        var canView = await service.DoesProfileHaveRoleAsync(
            new ProfileId(profileId),
            "CanSendByEboks");

        Assert.False(canView);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<HttpResponseMessage> PostJsonAsync(string path, object body)
    {
        var json    = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(path, content, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> GetWithToken(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
