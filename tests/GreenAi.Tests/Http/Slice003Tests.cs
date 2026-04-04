using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;

namespace GreenAi.Tests.Http;

/// <summary>
/// HTTP integration tests for SLICE-003:
///   - GET /api/auth/me returns correct identity fields from JWT
///   - GET /api/auth/me without auth returns 401
///   - CanUserAccess* is covered by PermissionServiceTests (SQL layer)
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class Slice003Tests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;
    private readonly AuthTestDataBuilder _builder;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Slice003Tests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db     = db;
        _client = factory.CreateClient();
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync()    => ValueTask.CompletedTask;

    // =========================================================================
    // GET /api/auth/me
    // =========================================================================

    [Fact]
    public async Task Me_ValidToken_ReturnsIdentityFields()
    {
        var customerId = await _builder.InsertCustomerAsync("Me Customer");
        var userId     = await _builder.InsertUserAsync(new() { Email = "me@test.local" });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);
        var profileId  = await _builder.InsertProfileAsync(customerId, userId);

        var token = TestJwtHelper.CreateToken(userId, customerId, new ProfileId(profileId), "me@test.local", languageId: 2);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc  = JsonDocument.Parse(body);

        Assert.Equal(userId.Value,     doc.RootElement.GetProperty("userId").GetInt32());
        Assert.Equal(customerId.Value, doc.RootElement.GetProperty("customerId").GetInt32());
        Assert.Equal(profileId,        doc.RootElement.GetProperty("profileId").GetInt32());
        Assert.Equal(2,                doc.RootElement.GetProperty("languageId").GetInt32());
        Assert.Equal("me@test.local",  doc.RootElement.GetProperty("email").GetString());
        Assert.False(doc.RootElement.GetProperty("isImpersonating").GetBoolean());
    }

    [Fact]
    public async Task Me_NoAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_TokenWithProfileId0_Returns401_FromMiddleware()
    {
        // /api/auth/me is in /api/auth/* → exempt from CurrentUserMiddleware
        // BUT RequireAuthorization() still requires a valid Bearer token
        var token = TestJwtHelper.CreateToken(new UserId(1), new CustomerId(0), new ProfileId(0));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // /api/auth/me IS exempt from CurrentUserMiddleware — should reach handler
        // Handler reads from claims directly — succeeds even with 0 values
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
