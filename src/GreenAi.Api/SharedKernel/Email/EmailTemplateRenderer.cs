namespace GreenAi.Api.SharedKernel.Email;

/// <summary>
/// Renders email template bodies by replacing {{placeholder}} tokens with supplied values.
///
/// Supported tokens: any key in the values dictionary, e.g. {{name}}, {{link}}, {{token}}, {{ttl}}.
/// Unknown tokens are left as-is (fail-open — never throws on missing keys).
/// Token matching is case-insensitive.
/// </summary>
public static class EmailTemplateRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template) || values.Count == 0)
            return template;

        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);

        return result;
    }
}
