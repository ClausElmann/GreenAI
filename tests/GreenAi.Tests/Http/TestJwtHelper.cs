using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.IdentityModel.Tokens;

namespace GreenAi.Tests.Http;

/// <summary>
/// Generates JWT tokens for HTTP integration tests.
///
/// Uses the same values as appsettings.Development.json so the tokens
/// will be accepted by the test host (GreenAiWebApplicationFactory).
///
/// See: docs/SSOT/testing/known-issues.md KI-005
/// </summary>
public static class TestJwtHelper
{
    // -----------------------------------------------------------------------
    // Must match appsettings.Development.json exactly.
    // GreenAiWebApplicationFactory sets ASPNETCORE_ENVIRONMENT=Development.
    // -----------------------------------------------------------------------
    private const string SecretKey  = "dev-secret-key-min-32-chars-long!!";
    private const string Issuer     = "greenai-dev";
    private const string Audience   = "greenai-dev";

    /// <summary>
    /// Creates a JWT access token with the given identity claims.
    /// ProfileId defaults to 0 (pre-profile-selection state).
    /// </summary>
    public static string CreateToken(
        UserId userId,
        CustomerId customerId,
        ProfileId profileId,
        string email = "test@example.com",
        int languageId = 1)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt   = DateTimeOffset.UtcNow.AddMinutes(60);

        var claims = new[]
        {
            new Claim(GreenAiClaims.Sub,        userId.Value.ToString()),
            new Claim(GreenAiClaims.Name,       email),
            new Claim(GreenAiClaims.Email,      email),
            new Claim(GreenAiClaims.CustomerId, customerId.Value.ToString()),
            new Claim(GreenAiClaims.ProfileId,  profileId.Value.ToString()),
            new Claim(GreenAiClaims.LanguageId, languageId.ToString()),
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer:            Issuer,
            audience:          Audience,
            claims:            claims,
            expires:           expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    /// <summary>
    /// Creates a token with ProfileId > 0 — used for IRequireProfile endpoints.
    /// </summary>
    public static string CreateFullToken(
        UserId userId,
        CustomerId customerId,
        ProfileId profileId,
        string email = "test@example.com")
        => CreateToken(userId, customerId, profileId, email);

    /// <summary>
    /// Creates an auth header value: "Bearer {token}".
    /// </summary>
    public static string BearerHeader(string token) => $"Bearer {token}";
}
