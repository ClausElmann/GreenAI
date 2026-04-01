namespace GreenAi.Api.Features.Auth.RefreshToken;

public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken);
