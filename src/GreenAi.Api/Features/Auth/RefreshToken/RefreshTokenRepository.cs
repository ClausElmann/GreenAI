using Dapper;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.RefreshToken;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenRecord?> FindValidTokenAsync(string token);
    Task RevokeTokenAsync(int tokenId);
    Task SaveNewTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId);
}

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbSession _db;

    public RefreshTokenRepository(IDbSession db) => _db = db;

    public Task<RefreshTokenRecord?> FindValidTokenAsync(string token)
        => _db.QuerySingleOrDefaultAsync<RefreshTokenRecord>(
            SqlLoader.Load<RefreshTokenRepository>("FindValidRefreshToken.sql"),
            new { Token = token, UtcNow = DateTimeOffset.UtcNow });

    public Task RevokeTokenAsync(int tokenId)
        => _db.ExecuteAsync(
            SqlLoader.Load<RefreshTokenRepository>("RevokeRefreshToken.sql"),
            new { TokenId = tokenId, UsedAt = DateTimeOffset.UtcNow });

    public Task SaveNewTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<RefreshTokenRepository>("SaveNewRefreshToken.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value, ProfileId = profileId.Value, Token = token, ExpiresAt = expiresAt, LanguageId = languageId });
}

public sealed record RefreshTokenRecord(
    int Id,
    int UserId,
    int CustomerId,
    int LanguageId,
    int ProfileId,
    string Email);
