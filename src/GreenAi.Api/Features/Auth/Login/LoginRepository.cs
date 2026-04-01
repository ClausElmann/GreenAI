using Dapper;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.Login;

public interface ILoginRepository
{
    Task<LoginUserRecord?> FindByEmailAsync(string email);
    Task RecordFailedLoginAsync(UserId userId);
    Task ResetFailedLoginAsync(UserId userId);
    Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId);
    Task<IEnumerable<UserMembershipRecord>> GetMembershipsAsync(UserId userId);
    Task<IReadOnlyCollection<ProfileRecord>> GetProfilesAsync(UserId userId, CustomerId customerId);
}

public sealed class LoginRepository : ILoginRepository
{
    private readonly IDbSession _db;

    public LoginRepository(IDbSession db) => _db = db;

    public Task<LoginUserRecord?> FindByEmailAsync(string email)
        => _db.QuerySingleOrDefaultAsync<LoginUserRecord>(
            SqlLoader.Load<LoginRepository>("FindUserByEmail.sql"),
            new { Email = email });

    public Task RecordFailedLoginAsync(UserId userId)
        => _db.ExecuteAsync(
            SqlLoader.Load<LoginRepository>("RecordFailedLogin.sql"),
            new { UserId = userId.Value, UtcNow = DateTimeOffset.UtcNow });

    public Task ResetFailedLoginAsync(UserId userId)
        => _db.ExecuteAsync(
            SqlLoader.Load<LoginRepository>("ResetFailedLogin.sql"),
            new { UserId = userId.Value });

    public Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<LoginRepository>("SaveRefreshToken.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value, ProfileId = profileId.Value, Token = token, ExpiresAt = expiresAt, LanguageId = languageId });

    public Task<IEnumerable<UserMembershipRecord>> GetMembershipsAsync(UserId userId)
        => _db.QueryAsync<UserMembershipRecord>(
            SqlLoader.Load<LoginRepository>("GetUserMemberships.sql"),
            new { UserId = userId.Value });

    public async Task<IReadOnlyCollection<ProfileRecord>> GetProfilesAsync(UserId userId, CustomerId customerId)
    {
        var results = await _db.QueryAsync<ProfileRecord>(
            SqlLoader.Load<LoginRepository>("GetProfiles.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value });
        return results.ToList();
    }
}

public sealed record LoginUserRecord(
    int Id,
    string Email,
    string PasswordHash,
    string PasswordSalt,
    int FailedLoginCount,
    bool IsLockedOut);

// Dapper result record — int fields follow existing codebase pattern (no TypeHandler registered).
// Strongly-typed ID wrapping is done at the handler level after post-auth tenant resolution.
public sealed record UserMembershipRecord(
    int CustomerId,
    string CustomerName,
    int LanguageId);
