using Dapper;
using GreenAi.Api.SharedKernel.Db;

namespace GreenAi.Api.Features.UserSelfService.PasswordReset;

public interface IPasswordResetConfirmRepository
{
    Task<PasswordResetTokenRecord?> FindTokenAsync(string token);
    Task ConfirmResetAsync(int tokenId, int userId, string passwordHash, string passwordSalt);
}

public sealed class PasswordResetConfirmRepository : IPasswordResetConfirmRepository
{
    private readonly IDbSession _db;

    public PasswordResetConfirmRepository(IDbSession db) => _db = db;

    public Task<PasswordResetTokenRecord?> FindTokenAsync(string token)
        => _db.QuerySingleOrDefaultAsync<PasswordResetTokenRecord>(
            SqlLoader.Load<PasswordResetConfirmRepository>("FindResetToken.sql"),
            new { Token = token });

    public async Task ConfirmResetAsync(int tokenId, int userId, string passwordHash, string passwordSalt)
    {
        await _db.ExecuteInTransactionAsync(async () =>
        {
            await _db.ExecuteAsync(
                SqlLoader.Load<PasswordResetConfirmRepository>("MarkTokenUsed.sql"),
                new { Id = tokenId });

            await _db.ExecuteAsync(
                SqlLoader.Load<PasswordResetConfirmRepository>("UpdatePassword.sql"),
                new { UserId = userId, PasswordHash = passwordHash, PasswordSalt = passwordSalt });
        });
    }
}

public sealed record PasswordResetTokenRecord(
    int            Id,
    int            UserId,
    string         Token,
    DateTimeOffset ExpiresAt);
