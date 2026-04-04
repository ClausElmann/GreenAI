using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Http;

/// <summary>
/// HTTP integration tests for SLICE-002:
///   - CurrentUserMiddleware: hard 401 when profileId=0 or customerId=0 on protected routes
///   - LogoutHandler: DELETE /api/auth/logout deletes all refresh tokens for the user
///
/// Auth routes (/api/auth/*) are exempt from the middleware guard.
/// Non-auth API routes require both customerId > 0 and profileId > 0.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class Slice002Tests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;
    private readonly AuthTestDataBuilder _builder;

    public Slice002Tests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db     = db;
        _client = factory.CreateClient();
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync()    => ValueTask.CompletedTask;

    // =========================================================================
    // CurrentUserMiddleware — C_001 + C_005
    // =========================================================================

    [Fact]
    public async Task ProtectedRoute_TokenWithProfileId0_Returns401()
    {
        // JWT has customerId > 0 but profileId = 0 — should be blocked
        var userId     = new UserId(1);
        var customerId = new CustomerId(10);
        var profileId  = new ProfileId(0);
        var token      = TestJwtHelper.CreateToken(userId, customerId, profileId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_TokenWithCustomerId0_Returns401()
    {
        // JWT has customerId = 0 but profileId > 0 — should be blocked
        var userId     = new UserId(1);
        var customerId = new CustomerId(0);
        var profileId  = new ProfileId(5);
        var token      = TestJwtHelper.CreateToken(userId, customerId, profileId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_TokenWithBothIds0_Returns401()
    {
        var token = TestJwtHelper.CreateToken(new UserId(1), new CustomerId(0), new ProfileId(0));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthRoute_TokenWithProfileId0_IsNotBlocked()
    {
        // /api/auth/* is exempt from the middleware guard — step-1 tokens must be allowed
        var token = TestJwtHelper.CreateToken(new UserId(1), new CustomerId(0), new ProfileId(0));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/select-customer")
        {
            Content = new StringContent("""{"customerId":1}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Not blocked by middleware — gets through to handler (may return 4xx from handler itself, but not 401 from middleware)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ProtectedRoute_NotBlockedByMiddleware()
    {
        // No Bearer token — standard ASP.NET auth handles this, not our middleware
        var response = await _client.GetAsync("/api/ping", TestContext.Current.CancellationToken);

        // /api/ping is not authenticated — should still return 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // =========================================================================
    // LogoutHandler — DELETE /api/auth/logout
    // =========================================================================

    [Fact]
    public async Task Logout_ValidToken_Returns204AndDeletesRefreshTokens()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync("Logout Customer");
        var userId     = await _builder.InsertUserAsync(new() { Email = "logout@test.local" });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId  = await _builder.InsertProfileAsync(customerId, userId, "Logout Profile");
        await _builder.InsertRefreshTokenAsync(customerId, userId, new() { Token = "token-to-delete" });

        var accessToken = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId));

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert — 204 returned
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert — token gone from DB
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM UserRefreshTokens WHERE UserId = @UserId",
            new { UserId = userId.Value });
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Logout_NoAuth_Returns401()
    {
        var response = await _client.DeleteAsync("/api/auth/logout", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
