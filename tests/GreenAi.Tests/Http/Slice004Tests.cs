using System.Net;
using System.Text.Json;
using Dapper;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Http;

/// <summary>
/// HTTP integration tests for SLICE-004:
///   - GET /api/localization/{languageId} returns label dictionary from DB
///   - Unknown languageId returns empty dictionary (fail-open — frontend falls back to key names)
///   - Route is public — no Authorization header required
///
/// Tests insert their own labels rather than relying on migration seed data,
/// making them robust across Respawn resets.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class Slice004Tests : IClassFixture<GreenAiWebApplicationFactory>, IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Slice004Tests(DatabaseFixture db, GreenAiWebApplicationFactory factory)
    {
        _db    = db;
        _client = factory.CreateClient();
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        // Clean up test-specific labels inserted by this fixture.
        // Real labels (shared.*, feature.*) are in TablesToIgnore and must never be deleted.
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        await conn.ExecuteAsync(
            "DELETE FROM [dbo].[Labels] WHERE [ResourceName] LIKE 'test.%'");
    }

    private static async Task InsertLabelAsync(string resourceName, string resourceValue, int languageId)
    {
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        // Idempotent: skip if already exists (unique index on LanguageId + ResourceName)
        await conn.ExecuteAsync(
            """
            IF NOT EXISTS (
                SELECT 1 FROM [dbo].[Labels]
                WHERE [LanguageId] = @LanguageId AND [ResourceName] = @ResourceName
            )
                INSERT INTO [dbo].[Labels] ([ResourceName], [ResourceValue], [LanguageId])
                VALUES (@ResourceName, @ResourceValue, @LanguageId)
            """,
            new { ResourceName = resourceName, ResourceValue = resourceValue, LanguageId = languageId });
    }

    // =========================================================================
    // GET /api/localization/{languageId}
    // =========================================================================

    [Fact]
    public async Task GetLabels_KnownLanguage_ReturnsDictionary()
    {
        // Arrange — insert a known label for LanguageId=1
        await InsertLabelAsync("test.saveButton", "Gem", 1);

        var response = await _client.GetAsync(
            "/api/localization/1",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var labels  = JsonSerializer.Deserialize<Dictionary<string, string>>(content, JsonOptions);

        Assert.NotNull(labels);
        Assert.True(labels.Count > 0, "Expected at least one label for LanguageId=1");
        Assert.Equal("Gem", labels["test.saveButton"]);
    }

    [Fact]
    public async Task GetLabels_UnknownLanguageId_ReturnsEmptyDictionary()
    {
        // LanguageId 999 has no labels — fail-open: returns empty dict
        var response = await _client.GetAsync(
            "/api/localization/999",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var labels  = JsonSerializer.Deserialize<Dictionary<string, string>>(content, JsonOptions);

        Assert.NotNull(labels);
        Assert.Empty(labels);
    }

    [Fact]
    public async Task GetLabels_NoAuthHeader_Returns200()
    {
        // Route is public — frontend bootstraps labels before login
        var response = await _client.GetAsync(
            "/api/localization/3",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLabels_TwoLanguages_ReturnSeparateValues()
    {
        // DA (LanguageId=1) = "Gem", EN (LanguageId=3) = "Save" — same key, different language
        await InsertLabelAsync("test.sharedButton", "Gem", 1);
        await InsertLabelAsync("test.sharedButton", "Save", 3);

        var daResponse = await _client.GetAsync("/api/localization/1", TestContext.Current.CancellationToken);
        var enResponse = await _client.GetAsync("/api/localization/3", TestContext.Current.CancellationToken);

        var daLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await daResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), JsonOptions);
        var enLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await enResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), JsonOptions);

        Assert.Equal("Gem",  daLabels!["test.sharedButton"]);
        Assert.Equal("Save", enLabels!["test.sharedButton"]);
    }
}
