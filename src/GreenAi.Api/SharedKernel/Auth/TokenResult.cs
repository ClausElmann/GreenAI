namespace GreenAi.Api.SharedKernel.Auth;

public sealed record TokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken
);
