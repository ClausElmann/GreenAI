using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Http.AdminLight;

/// <summary>
/// HTTP integration tests for P2-SLICE-003 (admin_light).
///
/// Coverage:
///   POST /api/admin/users              — CreateUser
///   POST /api/admin/users/{id}/roles   — AssignRole
///   POST /api/admin/users/{id}/profiles — AssignProfile
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class AdminLightTests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture     _db;
    private readonly HttpClient          _client;
    private readonly AuthTestDataBuilder _builder;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public AdminLightTests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db      = db;
        _client  = factory.CreateClient();
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync()    => ValueTask.CompletedTask;

    // =========================================================================
    // Helper methods
    // =========================================================================

    private Task<HttpResponseMessage> PostJsonAsync(string url, object body, string? bearerToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        if (bearerToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Seeds an admin user with ManageUsers + ManageProfiles roles and returns a full JWT token.
    /// </summary>
    private async Task<(UserId UserId, CustomerId CustomerId, int ProfileId, string Token)> SeedAdminAsync()
    {
        var (hash, salt) = PasswordHasher.Hash("adminPass123");
        var customerId   = await _builder.InsertCustomerAsync("Admin Customer");
        var userId       = await _builder.InsertUserAsync(new() { Email = "admin@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId, languageId: 1);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Admin Profile");

        // Grant ManageUsers + ManageProfiles roles
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO UserRoleMappings (UserId, UserRoleId)
            SELECT @UserId, Id FROM UserRoles WHERE Name IN ('ManageUsers', 'ManageProfiles')
            """, new { UserId = userId.Value });

        var token = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId), "admin@test.local");
        return (userId, customerId, profileId, token);
    }

    // =========================================================================
    // CREATE USER — POST /api/admin/users
    // =========================================================================

    [Fact]
    public async Task CreateUser_ValidInput_Returns200WithNewUserId()
    {
        var (_, _, _, token) = await SeedAdminAsync();

        var response = await PostJsonAsync("/api/admin/users", new
        {
            email           = "newbie@test.local",
            initialPassword = "Pass1234!",
            languageId      = 1
        }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc  = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("userId").GetProperty("value").GetInt32() > 0,
            "Expected a non-zero userId in response.");
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns409()
    {
        var (_, _, _, token) = await SeedAdminAsync();

        // First create
        await PostJsonAsync("/api/admin/users", new
        {
            email           = "duplicate@test.local",
            initialPassword = "Pass1234!"
        }, token);

        // Second create — same email
        var response = await PostJsonAsync("/api/admin/users", new
        {
            email           = "duplicate@test.local",
            initialPassword = "Pass1234!"
        }, token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_InvalidEmail_Returns400()
    {
        var (_, _, _, token) = await SeedAdminAsync();

        var response = await PostJsonAsync("/api/admin/users", new
        {
            email           = "not-an-email",
            initialPassword = "Pass1234!"
        }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_NoToken_Returns401()
    {
        var response = await PostJsonAsync("/api/admin/users", new
        {
            email           = "newbie@test.local",
            initialPassword = "Pass1234!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_CallerLacksManageUsers_Returns403()
    {
        // Seed a user WITHOUT ManageUsers role
        var (hash, salt) = PasswordHasher.Hash("regularPass123");
        var customerId   = await _builder.InsertCustomerAsync("Other Customer");
        var userId       = await _builder.InsertUserAsync(new() { Email = "regular@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Regular Profile");
        var token = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId), "regular@test.local");

        var response = await PostJsonAsync("/api/admin/users", new
        {
            email           = "another@test.local",
            initialPassword = "Pass1234!"
        }, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // ASSIGN ROLE — POST /api/admin/users/{id}/roles
    // =========================================================================

    [Fact]
    public async Task AssignRole_ValidRole_Returns200()
    {
        var (adminUserId, customerId, profileId, token) = await SeedAdminAsync();

        // Create target user first
        var (hash, salt) = PasswordHasher.Hash("targetPass");
        var targetUserId = await _builder.InsertUserAsync(new() { Email = "target-role@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(targetUserId, customerId);

        var response = await PostJsonAsync(
            $"/api/admin/users/{targetUserId.Value}/roles",
            new { roleName = UserRoleNames.ManageUsers },
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_NonExistentRole_Returns404()
    {
        var (_, _, _, token) = await SeedAdminAsync();

        var (hash, salt) = PasswordHasher.Hash("targetPass");
        var targetUserId = await _builder.InsertUserAsync(new() { Email = "target-role2@test.local", PasswordHash = hash, PasswordSalt = salt });

        var response = await PostJsonAsync(
            $"/api/admin/users/{targetUserId.Value}/roles",
            new { roleName = "GhostRole" },
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_IdempotentSecondAssign_Returns200()
    {
        var (_, customerId, _, token) = await SeedAdminAsync();

        var (hash, salt) = PasswordHasher.Hash("targetPass");
        var targetUserId = await _builder.InsertUserAsync(new() { Email = "target-role3@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(targetUserId, customerId);

        var body = new { roleName = UserRoleNames.ManageUsers };
        var url  = $"/api/admin/users/{targetUserId.Value}/roles";

        await PostJsonAsync(url, body, token);
        var r2 = await PostJsonAsync(url, body, token);

        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }

    // =========================================================================
    // ASSIGN PROFILE — POST /api/admin/users/{id}/profiles
    // =========================================================================

    [Fact]
    public async Task AssignProfile_ValidInput_Returns200()
    {
        var (_, customerId, _, token) = await SeedAdminAsync();

        var (hash, salt) = PasswordHasher.Hash("targetPass");
        var targetUserId = await _builder.InsertUserAsync(new() { Email = "target-profile@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(targetUserId, customerId);

        // Create a second profile on the same customer
        var newProfileId = await _builder.InsertProfileAsync(customerId, targetUserId, "New Profile");

        var response = await PostJsonAsync(
            $"/api/admin/users/{targetUserId.Value}/profiles",
            new { profileId = newProfileId },
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AssignProfile_ProfileFromAnotherCustomer_Returns404()
    {
        var (_, _, _, token) = await SeedAdminAsync();

        // Profile in a completely different customer
        var otherCustomerId = await _builder.InsertCustomerAsync("Other Customer");
        var (hash, salt)    = PasswordHasher.Hash("otherPass");
        var otherUserId     = await _builder.InsertUserAsync(new() { Email = "other@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(otherUserId, otherCustomerId);
        var foreignProfileId = await _builder.InsertProfileAsync(otherCustomerId, otherUserId, "Foreign Profile");

        var response = await PostJsonAsync(
            $"/api/admin/users/{otherUserId.Value}/profiles",
            new { profileId = foreignProfileId },
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignProfile_UserNotInCustomer_Returns404()
    {
        var (_, customerId, _, token) = await SeedAdminAsync();

        // Create a profile in the admin's customer
        var (h, s)     = PasswordHasher.Hash("somePass");
        var tempUserId = await _builder.InsertUserAsync(new() { Email = "temp@test.local", PasswordHash = h, PasswordSalt = s });
        await _builder.InsertUserCustomerMembershipAsync(tempUserId, customerId);
        var profileId  = await _builder.InsertProfileAsync(customerId, tempUserId, "Some Profile");

        // Target user is in a DIFFERENT customer
        var otherCustomerId  = await _builder.InsertCustomerAsync("Other Customer 2");
        var (h2, s2)         = PasswordHasher.Hash("anotherPass");
        var outsiderUserId   = await _builder.InsertUserAsync(new() { Email = "outsider@test.local", PasswordHash = h2, PasswordSalt = s2 });
        await _builder.InsertUserCustomerMembershipAsync(outsiderUserId, otherCustomerId);

        var response = await PostJsonAsync(
            $"/api/admin/users/{outsiderUserId.Value}/profiles",
            new { profileId },
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
