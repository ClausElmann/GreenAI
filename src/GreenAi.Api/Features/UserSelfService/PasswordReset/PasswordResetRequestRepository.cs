using Dapper;
using GreenAi.Api.SharedKernel.Db;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public interface IPasswordResetRequestRepository
{
    Task<PasswordResetUserRecord?> FindUserByEmailAsync(string email);
    Task InsertTokenAsync(int userId, string token, DateTimeOffset expiresAt);
}

public sealed class PasswordResetRequestRepository : IPasswordResetRequestRepository
{
    private readonly IDbSession _db;

    public PasswordResetRequestRepository(IDbSession db) => _db = db;

    public Task<PasswordResetUserRecord?> FindUserByEmailAsync(string email)
        => _db.QuerySingleOrDefaultAsync<PasswordResetUserRecord>(
            SqlLoader.Load<PasswordResetRequestRepository>("FindUserByEmail.sql"),
            new { Email = email });

    public Task InsertTokenAsync(int userId, string token, DateTimeOffset expiresAt)
        => _db.ExecuteAsync(
            SqlLoader.Load<PasswordResetRequestRepository>("InsertResetToken.sql"),
            new { UserId = userId, Token = token, ExpiresAt = expiresAt });
}

public sealed record PasswordResetUserRecord(int UserId, string Email);
