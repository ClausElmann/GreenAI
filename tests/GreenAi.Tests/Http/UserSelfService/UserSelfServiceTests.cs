using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Http.UserSelfService;

/// <summary>
/// HTTP integration tests for P2-SLICE-001 (user_self_service).
///
/// Coverage:
///   PUT  /api/user/update                — UpdateUser (DisplayName + LanguageId)
///   POST /api/user/password-reset-request — PasswordResetRequest (anonymous)
///   POST /api/user/password-reset-confirm — PasswordResetConfirm (anonymous)
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class UserSelfServiceTests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture     _db;
    private readonly HttpClient          _client;
    private readonly AuthTestDataBuilder _builder;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public UserSelfServiceTests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
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

    private async Task<(UserId UserId, CustomerId CustomerId, int ProfileId, string Token)> SeedUserAndLoginAsync(
        string email    = "self@test.local",
        string password = "testPass123")
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        var customerId   = await _builder.InsertCustomerAsync("SelfService Customer");
        var userId       = await _builder.InsertUserAsync(new() { Email = email, PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId, languageId: 1);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "Self Profile");

        // Full token with customerId + profileId
        var token = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId), email, languageId: 1);
        return (userId, customerId, profileId, token);
    }

    // =========================================================================
    // UPDATE USER — PUT /api/user/update
    // =========================================================================

    [Fact]
    public async Task UpdateUser_DisplayName_Returns200()
    {
        var (_, _, _, token) = await SeedUserAndLoginAsync();

        var response = await PutJsonAsync("/api/user/update",
            new { displayName = "Updated Name" }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_LanguageId_Returns200()
    {
        var (_, _, _, token) = await SeedUserAndLoginAsync();

        var response = await PutJsonAsync("/api/user/update",
            new { languageId = 3 }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_BothFields_Returns200()
    {
        var (_, _, _, token) = await SeedUserAndLoginAsync();

        var response = await PutJsonAsync("/api/user/update",
            new { displayName = "Bob", languageId = 2 }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_NoFields_Returns400()
    {
        var (_, _, _, token) = await SeedUserAndLoginAsync();

        var response = await PutJsonAsync("/api/user/update",
            new { }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_NoToken_Returns401()
    {
        var response = await PutJsonAsync("/api/user/update",
            new { displayName = "Alice" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // PASSWORD RESET REQUEST — POST /api/user/password-reset-request
    // =========================================================================

    [Fact]
    public async Task PasswordResetRequest_KnownEmail_Returns200()
    {
        var (_, _, _, _) = await SeedUserAndLoginAsync("reset@test.local");

        var response = await PostJsonAsync("/api/user/password-reset-request",
            new { email = "reset@test.local" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PasswordResetRequest_KnownEmail_InsertsTokenInDb()
    {
        await SeedUserAndLoginAsync("tokencheck@test.local");

        await PostJsonAsync("/api/user/password-reset-request",
            new { email = "tokencheck@test.local" });

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var count = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM PasswordResetTokens
            WHERE UsedAt IS NULL
              AND ExpiresAt > SYSDATETIMEOFFSET()
            """);

        Assert.True(count > 0, "Expected at least one valid token in PasswordResetTokens.");
    }

    [Fact]
    public async Task PasswordResetRequest_UnknownEmail_Returns200()
    {
        // Always 200 — prevent email enumeration
        var response = await PostJsonAsync("/api/user/password-reset-request",
            new { email = "nobody@test.local" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PasswordResetRequest_InvalidEmail_Returns400()
    {
        var response = await PostJsonAsync("/api/user/password-reset-request",
            new { email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // PASSWORD RESET CONFIRM — POST /api/user/password-reset-confirm
    // =========================================================================

    [Fact]
    public async Task PasswordResetConfirm_ValidToken_Returns200()
    {
        var (userId, _, _, _) = await SeedUserAndLoginAsync("confirm@test.local");

        var token     = new string('b', 64);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO PasswordResetTokens (UserId, Token, ExpiresAt)
            VALUES (@UserId, @Token, @ExpiresAt)
            """, new { UserId = userId.Value, Token = token, ExpiresAt = expiresAt });

        var response = await PostJsonAsync("/api/user/password-reset-confirm", new
        {
            token           = token,
            newPassword     = "newSecurePass!1",
            confirmPassword = "newSecurePass!1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PasswordResetConfirm_ValidToken_MarksTokenUsed()
    {
        var (userId, _, _, _) = await SeedUserAndLoginAsync("markused@test.local");

        var token     = new string('c', 64);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO PasswordResetTokens (UserId, Token, ExpiresAt)
            VALUES (@UserId, @Token, @ExpiresAt)
            """, new { UserId = userId.Value, Token = token, ExpiresAt = expiresAt });

        await PostJsonAsync("/api/user/password-reset-confirm", new
        {
            token           = token,
            newPassword     = "newSecurePass!1",
            confirmPassword = "newSecurePass!1"
        });

        var usedAt = await conn.ExecuteScalarAsync<DateTimeOffset?>(
            "SELECT UsedAt FROM PasswordResetTokens WHERE Token = @Token",
            new { Token = token });

        Assert.NotNull(usedAt);
    }

    [Fact]
    public async Task PasswordResetConfirm_InvalidToken_Returns400()
    {
        var response = await PostJsonAsync("/api/user/password-reset-confirm", new
        {
            token           = new string('z', 64),
            newPassword     = "newSecurePass!1",
            confirmPassword = "newSecurePass!1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PasswordResetConfirm_ExpiredToken_Returns400()
    {
        var (userId, _, _, _) = await SeedUserAndLoginAsync("expired@test.local");

        var token     = new string('e', 64);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(-1); // already expired

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO PasswordResetTokens (UserId, Token, ExpiresAt)
            VALUES (@UserId, @Token, @ExpiresAt)
            """, new { UserId = userId.Value, Token = token, ExpiresAt = expiresAt });

        var response = await PostJsonAsync("/api/user/password-reset-confirm", new
        {
            token           = token,
            newPassword     = "newSecurePass!1",
            confirmPassword = "newSecurePass!1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PasswordResetConfirm_PasswordMismatch_Returns400()
    {
        var response = await PostJsonAsync("/api/user/password-reset-confirm", new
        {
            token           = new string('f', 64),
            newPassword     = "pass1",
            confirmPassword = "pass2"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PasswordResetConfirm_ValidToken_AllowsLoginWithNewPassword()
    {
        var email = "relogin@test.local";
        var (userId, _, _, _) = await SeedUserAndLoginAsync(email, "oldPass123");

        var token     = new string('d', 64);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync("""
            INSERT INTO PasswordResetTokens (UserId, Token, ExpiresAt)
            VALUES (@UserId, @Token, @ExpiresAt)
            """, new { UserId = userId.Value, Token = token, ExpiresAt = expiresAt });

        // Reset password
        var confirmResponse = await PostJsonAsync("/api/user/password-reset-confirm", new
        {
            token           = token,
            newPassword     = "newSecurePass!1",
            confirmPassword = "newSecurePass!1"
        });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Try to login with new password
        var loginResponse = await PostJsonAsync("/api/auth/login", new
        {
            email    = email,
            password = "newSecurePass!1"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }
}
