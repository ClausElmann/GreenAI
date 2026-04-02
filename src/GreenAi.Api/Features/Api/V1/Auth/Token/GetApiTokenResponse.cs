namespace GreenAi.Api.Features.Api.V1.Auth.Token;

public record GetApiTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken);
