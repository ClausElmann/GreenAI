using System.Net;
using System.Net.Mail;
using GreenAi.Api.SharedKernel.Settings;

namespace GreenAi.Api.SharedKernel.Email;

/// <summary>
/// SMTP email service backed by Simply.com (or any standard SMTP relay).
///
/// Configuration is read from ApplicationSettings at send-time (hot-reload safe):
///   SmtpHost            — e.g. mail.simply.com
///   SmtpPort            — e.g. 587
///   SmtpUseSsl          — "true" / "false"
///   SmtpFromAddress     — sender address, e.g. noreply@example.com
///   SmtpFromName        — display name, e.g. "green-ai"
///   SmtpUsername        — SMTP auth username (usually same as FromAddress)
///   SmtpPassword        — SMTP auth password
///
/// Template is loaded from DB via EmailTemplateRepository using templateName + languageId=1 (DA).
/// Use languageId from caller to get the correct language (future: pass languageId to SendAsync).
///
/// Failures are not silently swallowed — exceptions propagate so the handler can return Result.Fail.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailTemplateRepository _templates;
    private readonly IApplicationSettingService _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    // Default language for templates: DA (1). Callers can override by loading templates directly.
    private const int DefaultLanguageId = 1;

    public SmtpEmailService(
        EmailTemplateRepository templates,
        IApplicationSettingService settings,
        ILogger<SmtpEmailService> logger)
    {
        _templates = templates;
        _settings  = settings;
        _logger    = logger;
    }

    public async Task SendAsync(
        string toAddress,
        string templateName,
        IReadOnlyDictionary<string, string> values,
        CancellationToken ct = default)
    {
        // Load template (DA fallback → EN)
        var template = await _templates.FindAsync(templateName, DefaultLanguageId)
            ?? throw new InvalidOperationException(
                $"Email template '{templateName}' not found for languageId={DefaultLanguageId} or EN fallback.");

        // Render subject + body
        var subject = EmailTemplateRenderer.Render(template.Subject, values);
        var body    = EmailTemplateRenderer.Render(template.BodyHtml, values);

        // Read SMTP config from ApplicationSettings
        var host        = await _settings.GetAsync(AppSetting.SmtpHost,        "mail.simply.com");
        var portStr     = await _settings.GetAsync(AppSetting.SmtpPort,        "587");
        var useSslStr   = await _settings.GetAsync(AppSetting.SmtpUseSsl,      "true");
        var fromAddress = await _settings.GetAsync(AppSetting.SmtpFromAddress, string.Empty);
        var fromName    = await _settings.GetAsync(AppSetting.SmtpFromName,    "green-ai");
        var username    = await _settings.GetAsync(AppSetting.SmtpUsername,    string.Empty);
        var password    = await _settings.GetAsync(AppSetting.SmtpPassword,    string.Empty);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException(
                "SMTP is not configured. Set SmtpHost and SmtpFromAddress in ApplicationSettings.");

        var port   = int.TryParse(portStr,  out var p) ? p : 587;
        var useSsl = bool.TryParse(useSslStr, out var s) ? s : true;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl            = useSsl,
            DeliveryMethod       = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials          = new NetworkCredential(username, password)
        };

        using var message = new MailMessage
        {
            From       = new MailAddress(fromAddress!, fromName),
            Subject    = subject,
            Body       = body,
            IsBodyHtml = true
        };
        message.To.Add(toAddress);

        _logger.LogInformation(
            "[EMAIL] Sending template={Template} to={To} via {Host}:{Port}",
            templateName, toAddress, host, port);

        await client.SendMailAsync(message, ct);

        _logger.LogInformation("[EMAIL] Sent template={Template} to={To}", templateName, toAddress);
    }
}
