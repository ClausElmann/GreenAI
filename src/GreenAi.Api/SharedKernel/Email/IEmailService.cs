namespace GreenAi.Api.SharedKernel.Email;

/// <summary>
/// Sends transactional emails from templates.
///
/// Real implementation (SmtpEmailService) is provided in P2-SLICE-002 (email_foundation).
/// Until then, NoOpEmailService is registered — tokens are generated and stored in DB
/// but no email is physically sent. Safe for development + test environments.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email using the named template, substituting the provided values.
    /// </summary>
    /// <param name="toAddress">Recipient email address.</param>
    /// <param name="templateName">Name of the EmailTemplate row in DB.</param>
    /// <param name="values">Key→value substitutions applied to template body + subject.</param>
    Task SendAsync(string toAddress, string templateName, IReadOnlyDictionary<string, string> values, CancellationToken ct = default);
}
