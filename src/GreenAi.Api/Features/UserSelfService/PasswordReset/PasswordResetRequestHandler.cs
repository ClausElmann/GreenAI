using System.Security.Cryptography;
using GreenAi.Api.SharedKernel.Email;
using GreenAi.Api.SharedKernel.Results;
using GreenAi.Api.SharedKernel.Settings;
using MediatR;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public sealed class PasswordResetRequestHandler
    : IRequestHandler<PasswordResetRequestCommand, Result<PasswordResetRequestResponse>>
{
    private readonly IPasswordResetRequestRepository _repository;
    private readonly IEmailService                   _emailService;
    private readonly IApplicationSettingService      _settings;

    private const string SuccessMessage =
        "If an account exists for that email, a password reset link has been sent.";

    public PasswordResetRequestHandler(
        IPasswordResetRequestRepository repository,
        IEmailService                   emailService,
        IApplicationSettingService      settings)
    {
        _repository   = repository;
        _emailService = emailService;
        _settings     = settings;
    }

    public async Task<Result<PasswordResetRequestResponse>> Handle(
        PasswordResetRequestCommand command, CancellationToken ct)
    {
        var user = await _repository.FindUserByEmailAsync(command.Email);

        // Always return success to prevent email enumeration
        if (user is null)
            return Result<PasswordResetRequestResponse>.Ok(new PasswordResetRequestResponse(SuccessMessage));

        // Generate cryptographically random token (64-char hex)
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        // Read TTL from settings (default: 60 minutes)
        var ttlStr     = await _settings.GetAsync(AppSetting.PasswordResetTokenTtlMinutes, "60");
        var ttlMinutes = int.TryParse(ttlStr, out var t) ? t : 60;
        var expiresAt  = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes);

        await _repository.InsertTokenAsync(user.UserId, token, expiresAt);

        // Build reset link
        var baseUrl = await _settings.GetAsync(AppSetting.PasswordResetBaseUrl, "https://localhost");
        var link    = $"{baseUrl}/reset-password?token={token}";

        var values = new Dictionary<string, string>
        {
            ["name"]  = user.Email,
            ["link"]  = link,
            ["token"] = token,
            ["ttl"]   = ttlMinutes.ToString()
        };

        await _emailService.SendAsync(user.Email, "password-reset", values, ct);

        return Result<PasswordResetRequestResponse>.Ok(new PasswordResetRequestResponse(SuccessMessage));
    }
}
