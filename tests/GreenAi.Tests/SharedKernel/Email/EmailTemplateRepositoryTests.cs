using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Email;

namespace GreenAi.Tests.SharedKernel.Email;

/// <summary>
/// Integration tests for EmailTemplateRepository.
/// Verifies DB round-trip: template seeded by V022 migration is found + returned correctly.
/// Fallback to EN (LanguageId=3) is tested via an unsupported languageId.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class EmailTemplateRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;

    public EmailTemplateRepositoryTests(DatabaseFixture db) => _db = db;

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync()    => ValueTask.CompletedTask;

    private static EmailTemplateRepository CreateRepo() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    [Fact]
    public async Task FindAsync_KnownTemplate_DA_ReturnsCorrectRow()
    {
        // V022 seeds 'password-reset' for DA (LanguageId=1) and EN (LanguageId=3)
        var repo   = CreateRepo();
        var result = await repo.FindAsync("password-reset", languageId: 1);

        Assert.NotNull(result);
        Assert.Equal("password-reset", result.Name);
        Assert.Equal(1, result.LanguageId);
        Assert.Contains("{{link}}", result.BodyHtml);
        Assert.Contains("{{name}}", result.BodyHtml);
        Assert.Contains("{{ttl}}",  result.BodyHtml);
    }

    [Fact]
    public async Task FindAsync_KnownTemplate_EN_ReturnsCorrectRow()
    {
        var repo   = CreateRepo();
        var result = await repo.FindAsync("password-reset", languageId: 3);

        Assert.NotNull(result);
        Assert.Equal(3, result.LanguageId);
        Assert.Contains("Reset your password", result.Subject);
    }

    [Fact]
    public async Task FindAsync_UnsupportedLanguage_FallsBackToEN()
    {
        // LanguageId=5 (NO) has no template seeded — should return EN fallback
        var repo   = CreateRepo();
        var result = await repo.FindAsync("password-reset", languageId: 5);

        Assert.NotNull(result);
        Assert.Equal(3, result.LanguageId); // EN fallback
    }

    [Fact]
    public async Task FindAsync_UnknownTemplate_ReturnsNull()
    {
        var repo   = CreateRepo();
        var result = await repo.FindAsync("nonexistent-template", languageId: 1);

        Assert.Null(result);
    }

    [Fact]
    public void Render_PasswordResetTemplate_ProducesValidHtml()
    {
        // Unit-level: renderer works with the actual template tokens
        var body = "Hi {{name}}, click {{link}} — valid for {{ttl}} min.";
        var values = new Dictionary<string, string>
        {
            ["name"] = "Alice",
            ["link"] = "https://app.example.com/reset?t=abc123",
            ["ttl"]  = "60"
        };

        var rendered = EmailTemplateRenderer.Render(body, values);

        Assert.Contains("Alice", rendered);
        Assert.Contains("https://app.example.com/reset?t=abc123", rendered);
        Assert.Contains("60",    rendered);
        Assert.DoesNotContain("{{", rendered); // all tokens replaced
    }
}
