using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Api.SharedKernel.Settings;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Http.AdminLight;

/// <summary>
/// HTTP integration tests for P2-SLICE-004 backend: ListSettings + SaveSetting.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SettingsTests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture     _db;
    private readonly HttpClient          _client;
    private readonly AuthTestDataBuilder _builder;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public SettingsTests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db      = db;
        _client  = factory.CreateClient();
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync()    => ValueTask.CompletedTask;

    // =========================================================================
    // Helpers
    // =========================================================================

    private Task<HttpResponseMessage> PutJsonAsync(string url, object body, string? bearerToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        if (bearerToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<string> SeedSuperAdminTokenAsync()
    {
        var (hash, salt) = PasswordHasher.Hash("superPass123");
        var customerId   = await _builder.InsertCustomerAsync("Settings Customer");
        var userId       = await _builder.InsertUserAsync(new() { Email = "superadmin@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "SuperAdmin Profile");

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO UserRoleMappings (UserId, UserRoleId)
            SELECT @UserId, Id FROM UserRoles WHERE Name = 'SuperAdmin'
            """, new { UserId = userId.Value });

        return TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId), "superadmin@test.local");
    }

    private async Task<string> SeedRegularUserTokenAsync()
    {
        var (hash, salt) = PasswordHasher.Hash("regularPass123");
        var customerId  = await _builder.InsertCustomerAsync("Regular Customer");
        var userId      = await _builder.InsertUserAsync(new() { Email = "regular-settings@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Regular Profile");
        return TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId), "regular-settings@test.local");
    }

    // =========================================================================
    // LIST SETTINGS — GET /api/admin/settings
    // =========================================================================

    [Fact]
    public async Task ListSettings_SuperAdmin_Returns200WithSettings()
    {
        var token = await SeedSuperAdminTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/settings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc  = JsonDocument.Parse(body);
        var settings = doc.RootElement.GetProperty("settings");
        Assert.True(settings.GetArrayLength() > 0, "Expected at least one setting in response.");
    }

    [Fact]
    public async Task ListSettings_NonSuperAdmin_Returns403()
    {
        var token = await SeedRegularUserTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/settings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListSettings_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/settings", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListSettings_SuperAdmin_ContainsAllEnumKeys()
    {
        var token = await SeedSuperAdminTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/settings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        var body     = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc      = JsonDocument.Parse(body);
        var settings = doc.RootElement.GetProperty("settings");

        var expectedCount = Enum.GetValues<AppSetting>().Length;
        Assert.Equal(expectedCount, settings.GetArrayLength());
    }

    // =========================================================================
    // SAVE SETTING — PUT /api/admin/settings/{key}
    // =========================================================================

    [Fact]
    public async Task SaveSetting_SuperAdmin_ValidKey_Returns200()
    {
        var token = await SeedSuperAdminTokenAsync();
        var key   = (int)AppSetting.SmtpFromName;

        var response = await PutJsonAsync(
            $"/api/admin/settings/{key}",
            new { value = "green-ai-test" },
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SaveSetting_SuperAdmin_InvalidKey_Returns404()
    {
        var token = await SeedSuperAdminTokenAsync();

        var response = await PutJsonAsync(
            "/api/admin/settings/99999",
            new { value = "whatever" },
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SaveSetting_NonSuperAdmin_Returns403()
    {
        var token = await SeedRegularUserTokenAsync();
        var key   = (int)AppSetting.SmtpFromName;

        var response = await PutJsonAsync(
            $"/api/admin/settings/{key}",
            new { value = "no-permission" },
            token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SaveSetting_SuperAdmin_ValuePersistedAndReadBack()
    {
        var token = await SeedSuperAdminTokenAsync();
        var key   = (int)AppSetting.SmtpFromName;

        await PutJsonAsync($"/api/admin/settings/{key}", new { value = "my-brand" }, token);

        // Read back via ListSettings
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/admin/settings");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getRequest, TestContext.Current.CancellationToken);

        var body     = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc      = JsonDocument.Parse(body);
        var settings = doc.RootElement.GetProperty("settings").EnumerateArray();

        var saved = settings.FirstOrDefault(s => s.GetProperty("key").GetInt32() == key);
        Assert.Equal("my-brand", saved.GetProperty("value").GetString());
    }
}
