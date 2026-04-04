namespace GreenAi.Api.SharedKernel.Settings;

/// <summary>
/// Strongly typed enum for alle systemkonfigurationsnøgler.
/// Numeriske IDs er stabile og svarer til ApplicationSettings.ApplicationSettingTypeId i DB.
/// Tilføj nyt felt + kald CreateDefault() i IApplicationSettingService ved ny nøgle.
/// Se docs/SSOT/backend/reference/appsetting-enum.md for kategoriseret reference.
/// </summary>
public enum AppSetting
{
    // Logging (1–9)
    RequestLogLevel             = 1,

    // SMTP (10–19)
    SmtpHost                    = 10,
    SmtpPort                    = 11,
    SmtpUseSsl                  = 12,
    SmtpFromAddress             = 13,
    SmtpFromName                = 14,
    SmtpUsername                = 15,
    SmtpPassword                = 16,

    // Password reset (20–29)
    PasswordResetTokenTtlMinutes = 20,
    PasswordResetBaseUrl        = 21,
}
