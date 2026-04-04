using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;

namespace GreenAi.Tests.Http;

/// <summary>
/// HTTP integration tests for all endpoint-bearing features.
///
/// Coverage: 9 features × (1 success + 1 invalid input) = 18 minimum tests.
///
/// Test host: GreenAiWebApplicationFactory (in-memory, real DB, Development config).
/// DB reset:  DatabaseFixture.ResetAsync() called per test via IAsyncLifetime.
///
/// Rules:
///   - Tests must be independent (Respawn resets data in InitializeAsync)
///   - JWT tokens generated via TestJwtHelper (matches appsettings.Development.json)
///   - No test relies on seed data from migrations (tests seed their own data)
///
/// See: docs/SSOT/testing/known-issues.md
/// See: docs/SSOT/testing/patterns/http-integration-test-pattern.md
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class HttpIntegrationTests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;
    private readonly AuthTestDataBuilder _builder;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HttpIntegrationTests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db      = db;
        _client  = factory.CreateClient();
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync()    => ValueTask.CompletedTask;

    // =========================================================================
    // Helper — POST JSON
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

    // =========================================================================
    // FEATURE: system_ping  —  GET /api/ping
    // =========================================================================

    [Fact]
    public async Task Ping_Get_Returns200()
    {
        var response = await _client.GetAsync("/api/ping", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_PostInsteadOfGet_Returns405()
    {
        var response = await _client.PostAsync("/api/ping", new StringContent("{}"), TestContext.Current.CancellationToken);
        // MinimalAPI does not allow POST on a GET-only route → 405 Method Not Allowed
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Ping_Returns200WithPongBody()
    {
        var response = await _client.GetAsync("/api/ping", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("pong", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ping_NoAuthRequired_Returns200()
    {
        // /api/ping is public — no Authorization header needed
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/ping");
        // intentionally no Authorization header
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_PutInsteadOfGet_Returns405()
    {
        var response = await _client.PutAsync("/api/ping", new StringContent("{}"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: auth_login  —  POST /api/auth/login
    // =========================================================================

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Arrange — seed a user with a known password
        var customerId = await _builder.InsertCustomerAsync("HTTP Test Customer");
        var (hash, salt) = PasswordHasher.Hash("correct-pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "http-login@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        await _builder.InsertProfileAsync(customerId, userId, "Test Profile");

        // Act
        var response = await PostJsonAsync("/api/auth/login", new { Email = "http-login@test.local", Password = "correct-pw" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc  = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("accessToken", out var token));
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange — seed user
        var customerId = await _builder.InsertCustomerAsync("HTTP Login Bad");
        var (hash, salt) = PasswordHasher.Hash("real-password");
        var userId = await _builder.InsertUserAsync(new() { Email = "http-bad-login@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        // Act — password must be ≥6 chars (LoginValidator) to pass validation and reach the handler
        var response = await PostJsonAsync("/api/auth/login", new { Email = "http-bad-login@test.local", Password = "wrong!" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        var response = await PostJsonAsync("/api/auth/login", new { Email = "not-an-email", Password = "password123" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShortPassword_Returns400()
    {
        // LoginValidator requires ≥6 chars
        var response = await PostJsonAsync("/api/auth/login", new { Email = "short@test.local", Password = "abc" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Valid format, passes validation, but user doesn't exist in DB
        var response = await PostJsonAsync("/api/auth/login", new { Email = "ghost@test.local", Password = "password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: auth_select_customer  —  POST /api/auth/select-customer
    // =========================================================================

    [Fact]
    public async Task SelectCustomer_ValidToken_Returns200WithNewToken()
    {
        // Arrange — user with membership in customerId 0-typed JWT
        var customerId = await _builder.InsertCustomerAsync("SC HTTP Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "sc-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        await _builder.InsertProfileAsync(customerId, userId, "SC Profile");

        // Issue a step-1 token (no CustomerId in JWT yet — ProfileId = 0)
        var token = TestJwtHelper.CreateToken(userId, new CustomerId(0), new ProfileId(0), "sc-http@test.local");

        // Act
        var response = await PostJsonAsync(
            "/api/auth/select-customer",
            new { CustomerId = customerId.Value },
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("accessToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectCustomer_NoAuth_Returns401()
    {
        var response = await PostJsonAsync("/api/auth/select-customer", new { CustomerId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SelectCustomer_MembershipNotFound_Returns403()
    {
        // Token is valid but user has no membership in the requested customer
        var userId   = new UserId(999);
        var customer = await _builder.InsertCustomerAsync("SC Non-Member Customer");
        var token    = TestJwtHelper.CreateToken(userId, new CustomerId(0), new ProfileId(0), "sc-nomember@test.local");

        var response = await PostJsonAsync("/api/auth/select-customer", new { CustomerId = customer.Value }, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SelectCustomer_CustomerIdZero_Returns400()
    {
        var (hash, salt) = PasswordHasher.Hash("pw");
        var customerId   = await _builder.InsertCustomerAsync("SC Zero Customer");
        var userId       = await _builder.InsertUserAsync(new() { Email = "sc-zero@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var token = TestJwtHelper.CreateToken(userId, new CustomerId(0), new ProfileId(0), "sc-zero@test.local");

        // CustomerId = 0 fails SelectCustomerValidator
        var response = await PostJsonAsync("/api/auth/select-customer", new { CustomerId = 0 }, token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: auth_select_profile  —  POST /api/auth/select-profile
    // =========================================================================

    [Fact]
    public async Task SelectProfile_ValidTokenAndProfile_Returns200WithFullToken()
    {
        // Arrange — user with customer membership + profile
        var customerId = await _builder.InsertCustomerAsync("SP HTTP Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "sp-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "SP Profile");

        // Issue a step-2 token (CustomerId set, ProfileId = 0)
        var token = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(0), "sp-http@test.local");

        // Act
        var response = await PostJsonAsync(
            "/api/auth/select-profile",
            new { ProfileId = profileId },
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("accessToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectProfile_NoAuth_Returns401()
    {
        var response = await PostJsonAsync("/api/auth/select-profile", new { ProfileId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SelectProfile_ProfileFromOtherCustomer_Returns403()
    {
        // Seed step-2 token for customer A, then request a profile that belongs to customer B
        var custA = await _builder.InsertCustomerAsync("SP CustA");
        var custB = await _builder.InsertCustomerAsync("SP CustB");
        var (h, s)   = PasswordHasher.Hash("pw");
        var userA    = await _builder.InsertUserAsync(new() { Email = "sp-wrong-cust@test.local", PasswordHash = h, PasswordSalt = s });
        var userB    = await _builder.InsertUserAsync(new() { Email = "sp-owner-b@test.local", PasswordHash = h, PasswordSalt = s });
        await _builder.InsertUserCustomerMembershipAsync(userA, custA);
        await _builder.InsertUserCustomerMembershipAsync(userB, custB);
        var foreignProfileId = await _builder.InsertProfileAsync(custB, userB, "B Profile");

        var token = TestJwtHelper.CreateToken(userA, custA, new ProfileId(0), "sp-wrong-cust@test.local");

        var response = await PostJsonAsync("/api/auth/select-profile", new { ProfileId = foreignProfileId }, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SelectProfile_InvalidProfileIdNegative_Returns400()
    {
        var customerId = await _builder.InsertCustomerAsync("SP Neg Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "sp-neg@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var token = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(0), "sp-neg@test.local");

        // ProfileId = -1 fails SelectProfileValidator (must be > 0 when provided)
        var response = await PostJsonAsync("/api/auth/select-profile", new { ProfileId = -1 }, token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: auth_refresh_token  —  POST /api/auth/refresh
    // =========================================================================

    [Fact]
    public async Task RefreshToken_ValidToken_Returns200WithNewTokens()
    {
        // Arrange — seed a refresh token row
        var customerId = await _builder.InsertCustomerAsync("RT HTTP Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "rt-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "RT Profile");

        const string refreshTokenValue = "http-test-refresh-token-abc123";
        await _builder.InsertRefreshTokenAsync(customerId, userId, new() { Token = refreshTokenValue, ProfileId = profileId });

        // Act
        var response = await PostJsonAsync("/api/auth/refresh", new { RefreshToken = refreshTokenValue });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("refreshToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        var response = await PostJsonAsync("/api/auth/refresh", new { RefreshToken = "non-existent-token" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_EmptyToken_Returns400()
    {
        // RefreshTokenValidator: NotEmpty
        var response = await PostJsonAsync("/api/auth/refresh", new { RefreshToken = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_MissingToken_Returns400()
    {
        var response = await PostJsonAsync("/api/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_IsConsumedAndNewTokenIssued()
    {
        // Use the refresh token once, then verify the old token can't be reused
        var customerId = await _builder.InsertCustomerAsync("RT Consume Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "rt-consume@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "RT Consume Profile");

        const string oldToken = "rt-consume-token-abcdef123456";
        await _builder.InsertRefreshTokenAsync(customerId, userId, new() { Token = oldToken, ProfileId = profileId });

        var first = await PostJsonAsync("/api/auth/refresh", new { RefreshToken = oldToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Reuse the same token — must fail (rotated)
        var second = await PostJsonAsync("/api/auth/refresh", new { RefreshToken = oldToken });
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    // =========================================================================
    // FEATURE: auth_change_password  —  POST /api/auth/change-password
    // =========================================================================

    [Fact]
    public async Task ChangePassword_ValidCredentials_Returns200()
    {
        // Arrange — seed user, membership, profile
        var customerId = await _builder.InsertCustomerAsync("CP HTTP Customer");
        var (hash, salt) = PasswordHasher.Hash("old-password");
        var userId = await _builder.InsertUserAsync(new() { Email = "cp-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CP Profile");

        // Issue a FULL token (ProfileId > 0) — required by IRequireProfile
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "cp-http@test.local");

        // Act
        var response = await PostJsonAsync(
            "/api/auth/change-password",
            new { CurrentPassword = "old-password", NewPassword = "new-password123!", ConfirmNewPassword = "new-password123!" },
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NoAuth_Returns401()
    {
        var response = await PostJsonAsync(
            "/api/auth/change-password",
            new { CurrentPassword = "x", NewPassword = "y", ConfirmNewPassword = "y" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns401()
    {
        var customerId = await _builder.InsertCustomerAsync("CP Wrong Customer");
        var (hash, salt) = PasswordHasher.Hash("real-password");
        var userId = await _builder.InsertUserAsync(new() { Email = "cp-wrong@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CP Wrong Profile");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "cp-wrong@test.local");

        var response = await PostJsonAsync(
            "/api/auth/change-password",
            new { CurrentPassword = "wrong-password", NewPassword = "new-password123!", ConfirmNewPassword = "new-password123!" },
            token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_PasswordMismatch_Returns400()
    {
        var customerId = await _builder.InsertCustomerAsync("CP Mismatch Customer");
        var (hash, salt) = PasswordHasher.Hash("real-pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "cp-mismatch@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CP Mismatch Profile");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "cp-mismatch@test.local");

        var response = await PostJsonAsync(
            "/api/auth/change-password",
            new { CurrentPassword = "real-pw", NewPassword = "new-pw-1!", ConfirmNewPassword = "different-pw!" },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShortNewPassword_Returns400()
    {
        var customerId = await _builder.InsertCustomerAsync("CP Short Customer");
        var (hash, salt) = PasswordHasher.Hash("real-pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "cp-short@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CP Short Profile");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "cp-short@test.local");

        // NewPassword < 6 chars fails ChangePasswordValidator
        var response = await PostJsonAsync(
            "/api/auth/change-password",
            new { CurrentPassword = "real-pw", NewPassword = "abc", ConfirmNewPassword = "abc" },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: identity_change_user_email  —  POST /api/identity/change-email
    // =========================================================================

    [Fact]
    public async Task ChangeUserEmail_NewUniqueEmail_Returns200()
    {
        // Arrange — seed user, membership, profile
        var customerId = await _builder.InsertCustomerAsync("CE HTTP Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "ce-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CE Profile");

        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "ce-http@test.local");

        // Act
        var response = await PostJsonAsync(
            "/api/identity/change-email",
            new { NewEmail = "ce-new@test.local", ConfirmNewEmail = "ce-new@test.local" },
            token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserEmail_NoAuth_Returns401()
    {
        var response = await PostJsonAsync(
            "/api/identity/change-email",
            new { NewEmail = "x@y.com", ConfirmNewEmail = "x@y.com" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserEmail_DuplicateEmail_Returns409()
    {
        // Seed two users — second tries to take first's email
        var customerId = await _builder.InsertCustomerAsync("CE Dup Customer");
        var (h1, s1) = PasswordHasher.Hash("pw");
        var existing = await _builder.InsertUserAsync(new() { Email = "already-taken@test.local", PasswordHash = h1, PasswordSalt = s1 });
        await _builder.InsertUserCustomerMembershipAsync(existing, customerId);

        var (h2, s2) = PasswordHasher.Hash("pw");
        var changer  = await _builder.InsertUserAsync(new() { Email = "changer@test.local", PasswordHash = h2, PasswordSalt = s2 });
        await _builder.InsertUserCustomerMembershipAsync(changer, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, changer, "CE Dup Profile");
        var token = TestJwtHelper.CreateFullToken(changer, customerId, new ProfileId(profileId), "changer@test.local");

        var response = await PostJsonAsync(
            "/api/identity/change-email",
            new { NewEmail = "already-taken@test.local", ConfirmNewEmail = "already-taken@test.local" },
            token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserEmail_EmailMismatch_Returns400()
    {
        var customerId = await _builder.InsertCustomerAsync("CE Mis Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "ce-mis@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CE Mis Profile");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "ce-mis@test.local");

        var response = await PostJsonAsync(
            "/api/identity/change-email",
            new { NewEmail = "new@test.local", ConfirmNewEmail = "different@test.local" },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserEmail_InvalidEmailFormat_Returns400()
    {
        var customerId = await _builder.InsertCustomerAsync("CE Fmt Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "ce-fmt@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "CE Fmt Profile");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "ce-fmt@test.local");

        var response = await PostJsonAsync(
            "/api/identity/change-email",
            new { NewEmail = "not-an-email", ConfirmNewEmail = "not-an-email" },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: localization_batch_upsert_labels  —  POST /api/labels/batch-upsert
    // =========================================================================

    [Fact]
    public async Task BatchUpsertLabels_ValidPayload_Returns200()
    {
        // Arrange — seed user with auth (IRequireAuthentication, no IRequireProfile needed)
        var customerId = await _builder.InsertCustomerAsync("BU HTTP Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "bu-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "BU Profile");

        // BatchUpsertLabels requires SuperAdmin role — assign before creating the token
        await _builder.AssignUserRoleAsync(userId, "SuperAdmin");

        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "bu-http@test.local");

        var payload = new
        {
            Labels = new[]
            {
                new { ResourceName = "test.http.label", ResourceValue = "HTTP test label", LanguageId = 1 }
            }
        };

        // Act
        var response = await PostJsonAsync("/api/labels/batch-upsert", payload, token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BatchUpsertLabels_NoAuth_Returns401()
    {
        var response = await PostJsonAsync(
            "/api/labels/batch-upsert",
            new { Labels = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BatchUpsertLabels_NotSuperAdmin_Returns403()
    {
        var customerId = await _builder.InsertCustomerAsync("BU ForbidCustomer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "bu-forbid@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "BU Forbid Profile");
        // No SuperAdmin role assigned
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "bu-forbid@test.local");

        var response = await PostJsonAsync("/api/labels/batch-upsert",
            new { Labels = new[] { new { ResourceName = "test.x", ResourceValue = "X", LanguageId = 1 } } },
            token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BatchUpsertLabels_EmptyBatch_Returns200WithZeroCount()
    {
        var customerId = await _builder.InsertCustomerAsync("BU Empty Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "bu-empty@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "BU Empty Profile");
        await _builder.AssignUserRoleAsync(userId, "SuperAdmin");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "bu-empty@test.local");

        var response = await PostJsonAsync("/api/labels/batch-upsert", new { Labels = Array.Empty<object>() }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("0", body); // upsertedCount = 0
    }

    [Fact]
    public async Task BatchUpsertLabels_InvalidLabel_Returns400()
    {
        var customerId = await _builder.InsertCustomerAsync("BU Invalid Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "bu-invalid@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "BU Invalid Profile");
        await _builder.AssignUserRoleAsync(userId, "SuperAdmin");
        var token = TestJwtHelper.CreateFullToken(userId, customerId, new ProfileId(profileId), "bu-invalid@test.local");

        // ResourceName is empty — fails BatchUpsertLabelsValidator
        var response = await PostJsonAsync("/api/labels/batch-upsert",
            new { Labels = new[] { new { ResourceName = "", ResourceValue = "X", LanguageId = 1 } } },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE: api_v1_get_token  —  POST /api/v1/auth/token
    // =========================================================================

    [Fact]
    public async Task GetApiToken_ValidApiCredentials_Returns200WithToken()
    {
        // Arrange — user with API role, membership, profile
        var customerId = await _builder.InsertCustomerAsync("API Token Customer");
        var (hash, salt) = PasswordHasher.Hash("api-pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "api-http@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "API Profile");
        await _builder.AssignUserRoleAsync(userId, "API");

        // Act
        var response = await PostJsonAsync("/api/v1/auth/token", new
        {
            Email      = "api-http@test.local",
            Password   = "api-pw",
            CustomerId = customerId.Value,
            ProfileId  = profileId
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("accessToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetApiToken_WrongPassword_Returns401()
    {
        var customerId = await _builder.InsertCustomerAsync("API Token Bad");
        var (hash, salt) = PasswordHasher.Hash("real-pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "api-bad@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        await _builder.InsertProfileAsync(customerId, userId, "API Bad Profile");
        await _builder.AssignUserRoleAsync(userId, "API");

        var response = await PostJsonAsync("/api/v1/auth/token", new
        {
            Email      = "api-bad@test.local",
            Password   = "wrong",
            CustomerId = customerId.Value,
            ProfileId  = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetApiToken_UserWithoutApiRole_Returns401()
    {
        var customerId = await _builder.InsertCustomerAsync("API No-Role Customer");
        var (hash, salt) = PasswordHasher.Hash("pw");
        var userId = await _builder.InsertUserAsync(new() { Email = "api-norole@test.local", PasswordHash = hash, PasswordSalt = salt });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId = await _builder.InsertProfileAsync(customerId, userId, "API No-Role Profile");
        // No API role — handler returns INVALID_CREDENTIALS

        var response = await PostJsonAsync("/api/v1/auth/token", new
        {
            Email      = "api-norole@test.local",
            Password   = "pw",
            CustomerId = customerId.Value,
            ProfileId  = profileId
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetApiToken_InvalidEmailFormat_Returns400()
    {
        var response = await PostJsonAsync("/api/v1/auth/token", new
        {
            Email      = "not-an-email",
            Password   = "pw",
            CustomerId = 1,
            ProfileId  = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetApiToken_CustomerIdZero_Returns400()
    {
        // GetApiTokenValidator: CustomerId must be > 0
        var response = await PostJsonAsync("/api/v1/auth/token", new
        {
            Email      = "api-zero@test.local",
            Password   = "pw",
            CustomerId = 0,
            ProfileId  = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
