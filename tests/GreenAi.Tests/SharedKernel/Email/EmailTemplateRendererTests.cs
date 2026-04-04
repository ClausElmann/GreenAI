using GreenAi.Api.SharedKernel.Email;

namespace GreenAi.Tests.SharedKernel.Email;

/// <summary>
/// Unit tests for EmailTemplateRenderer.
/// Verifies {{placeholder}} substitution, case-insensitivity, and fail-open behaviour.
/// </summary>
public sealed class EmailTemplateRendererTests
{
    [Fact]
    public void Render_SingleToken_ReplacesCorrectly()
    {
        var result = EmailTemplateRenderer.Render(
            "Hello {{name}}!",
            new Dictionary<string, string> { ["name"] = "Alice" });

        Assert.Equal("Hello Alice!", result);
    }

    [Fact]
    public void Render_MultipleTokens_ReplacesAll()
    {
        var result = EmailTemplateRenderer.Render(
            "Hi {{name}}, your token is {{token}} and it expires in {{ttl}} minutes.",
            new Dictionary<string, string>
            {
                ["name"]  = "Bob",
                ["token"] = "abc123",
                ["ttl"]   = "60"
            });

        Assert.Equal("Hi Bob, your token is abc123 and it expires in 60 minutes.", result);
    }

    [Fact]
    public void Render_TokenCaseInsensitive_Replaces()
    {
        var result = EmailTemplateRenderer.Render(
            "Link: {{LINK}}",
            new Dictionary<string, string> { ["link"] = "https://example.com/reset" });

        Assert.Equal("Link: https://example.com/reset", result);
    }

    [Fact]
    public void Render_UnknownToken_LeftAsIs()
    {
        // Fail-open: unknown placeholders are preserved, not removed
        var result = EmailTemplateRenderer.Render(
            "Hello {{unknown}}",
            new Dictionary<string, string> { ["name"] = "Alice" });

        Assert.Equal("Hello {{unknown}}", result);
    }

    [Fact]
    public void Render_EmptyTemplate_ReturnsEmpty()
    {
        var result = EmailTemplateRenderer.Render("", new Dictionary<string, string>());
        Assert.Equal("", result);
    }

    [Fact]
    public void Render_EmptyValues_ReturnsTemplateUnchanged()
    {
        var result = EmailTemplateRenderer.Render(
            "Hello {{name}}",
            new Dictionary<string, string>());

        Assert.Equal("Hello {{name}}", result);
    }

    [Fact]
    public void Render_HtmlBody_PreservesHtmlTags()
    {
        var result = EmailTemplateRenderer.Render(
            "<p>Hi {{name}},</p><p><a href=\"{{link}}\">Reset</a></p>",
            new Dictionary<string, string>
            {
                ["name"] = "Carol",
                ["link"] = "https://example.com/reset?t=xyz"
            });

        Assert.Equal(
            "<p>Hi Carol,</p><p><a href=\"https://example.com/reset?t=xyz\">Reset</a></p>",
            result);
    }
}
