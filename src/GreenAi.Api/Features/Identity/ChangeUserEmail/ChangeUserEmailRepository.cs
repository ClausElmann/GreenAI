using Dapper;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Identity.ChangeUserEmail;

public interface IChangeUserEmailRepository
{
    Task<bool> IsEmailAvailableAsync(string email, UserId excludeUserId);
    Task UpdateEmailAndAuditAsync(UserId userId, CustomerId customerId, string newEmail);
}

public sealed class ChangeUserEmailRepository : IChangeUserEmailRepository
{
    private readonly IDbSession _db;

    public ChangeUserEmailRepository(IDbSession db) => _db = db;

    public async Task<bool> IsEmailAvailableAsync(string email, UserId excludeUserId)
    {
        var count = await _db.QuerySingleOrDefaultAsync<int>(
            SqlLoader.Load<ChangeUserEmailRepository>("CheckEmailAvailable.sql"),
            new { Email = email, ExcludeUserId = excludeUserId.Value });

        return count == 0;
    }

    public Task UpdateEmailAndAuditAsync(UserId userId, CustomerId customerId, string newEmail)
        => _db.ExecuteInTransactionAsync(async () =>
        {
            await _db.ExecuteAsync(
                SqlLoader.Load<ChangeUserEmailRepository>("UpdateUserEmail.sql"),
                new { UserId = userId.Value, NewEmail = newEmail });

            await _db.ExecuteAsync(
                SqlLoader.Load<ChangeUserEmailRepository>("InsertAuditEntry.sql"),
                new
                {
                    CustomerId = customerId.Value,
                    UserId     = userId.Value,
                    ActorId    = userId.Value,
                    Action     = "EMAIL_CHANGED",
                    Details    = $"new={newEmail}"
                });
        });
}
