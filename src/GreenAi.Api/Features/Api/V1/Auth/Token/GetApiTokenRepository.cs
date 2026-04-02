using Dapper;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Api.V1.Auth.Token;

public interface IGetApiTokenRepository
{
    Task<ApiTokenUserRecord?> FindUserAsync(string email, int customerId, int profileId);
    Task RecordFailedLoginAsync(UserId userId);
    Task ResetFailedLoginAsync(UserId userId);
    Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId);
}

public sealed class GetApiTokenRepository : IGetApiTokenRepository
{
    private readonly IDbSession _db;

    public GetApiTokenRepository(IDbSession db) => _db = db;

    public Task<ApiTokenUserRecord?> FindUserAsync(string email, int customerId, int profileId)
        => _db.QuerySingleOrDefaultAsync<ApiTokenUserRecord>(
            SqlLoader.Load<GetApiTokenRepository>("GetApiTokenUserRecord.sql"),
            new { Email = email, CustomerId = customerId, ProfileId = profileId });

    public Task RecordFailedLoginAsync(UserId userId)
        => _db.ExecuteAsync(
            SqlLoader.Load<GetApiTokenRepository>("RecordFailedLogin.sql"),
            new { UserId = userId.Value, UtcNow = DateTimeOffset.UtcNow });

    public Task ResetFailedLoginAsync(UserId userId)
        => _db.ExecuteAsync(
            SqlLoader.Load<GetApiTokenRepository>("ResetFailedLogin.sql"),
            new { UserId = userId.Value });

    public Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<GetApiTokenRepository>("SaveRefreshToken.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value, ProfileId = profileId.Value, Token = token, ExpiresAt = expiresAt, LanguageId = languageId });
}

public sealed record ApiTokenUserRecord(
    int Id,
    string Email,
    string PasswordHash,
    string PasswordSalt,
    bool IsLockedOut,
    int FailedLoginCount,
    int LanguageId);
