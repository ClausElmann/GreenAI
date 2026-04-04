using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.AdminLight.CreateUser;

public interface ICreateUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<UserId> InsertUserAsync(string email, string passwordHash, string passwordSalt);
    Task InsertMembershipAsync(UserId userId, CustomerId customerId, int languageId);
}

public sealed class CreateUserRepository : ICreateUserRepository
{
    private readonly IDbSession _db;

    public CreateUserRepository(IDbSession db) => _db = db;

    public async Task<bool> EmailExistsAsync(string email)
    {
        var count = await _db.QuerySingleOrDefaultAsync<int>(
            SqlLoader.Load<CreateUserRepository>("FindUserByEmail.sql"),
            new { Email = email });
        return count > 0;
    }

    public async Task<UserId> InsertUserAsync(string email, string passwordHash, string passwordSalt)
    {
        var id = await _db.QuerySingleOrDefaultAsync<int>(
            SqlLoader.Load<CreateUserRepository>("InsertUser.sql"),
            new { Email = email, PasswordHash = passwordHash, PasswordSalt = passwordSalt });
        return new UserId(id);
    }

    public Task InsertMembershipAsync(UserId userId, CustomerId customerId, int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<CreateUserRepository>("InsertUserCustomerMembership.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value, LanguageId = languageId });
}
