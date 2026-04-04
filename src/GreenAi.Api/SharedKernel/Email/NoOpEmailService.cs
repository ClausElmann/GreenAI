namespace GreenAi.Api.SharedKernel.Email;

/// <summary>
/// No-op email service — logs the send attempt but does not deliver any email.
///
/// Active until P2-SLICE-002 (email_foundation) provides SmtpEmailService.
/// Registered as the default in Program.cs until real SMTP is configured.
/// </summary>
public sealed class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    public Task SendAsync(string toAddress, string templateName, IReadOnlyDictionary<string, string> values, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EMAIL-NOOP] Template={Template} To={To} — email_foundation (P2-SLICE-002) not yet implemented",
            templateName, toAddress);

        return Task.CompletedTask;
    }
}
