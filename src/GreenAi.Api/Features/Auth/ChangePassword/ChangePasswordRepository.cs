using Dapper;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.ChangePassword;

public interface IChangePasswordRepository
{
    Task<ChangePasswordUserRecord?> FindByUserIdAsync(UserId userId);
    Task UpdatePasswordAsync(UserId userId, string newHash, string newSalt);
}

public sealed class ChangePasswordRepository : IChangePasswordRepository
{
    private readonly IDbSession _db;

    public ChangePasswordRepository(IDbSession db) => _db = db;

    public Task<ChangePasswordUserRecord?> FindByUserIdAsync(UserId userId)
        => _db.QuerySingleOrDefaultAsync<ChangePasswordUserRecord>(
            SqlLoader.Load<ChangePasswordRepository>("GetUserCredentials.sql"),
            new { UserId = userId.Value });

    public Task UpdatePasswordAsync(UserId userId, string newHash, string newSalt)
        => _db.ExecuteAsync(
            SqlLoader.Load<ChangePasswordRepository>("UpdatePassword.sql"),
            new { UserId = userId.Value, PasswordHash = newHash, PasswordSalt = newSalt });
}

public sealed record ChangePasswordUserRecord(
    int Id,
    string PasswordHash,
    string PasswordSalt,
    bool IsLockedOut);
