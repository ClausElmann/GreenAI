using Dapper;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public sealed class SelectCustomerRepository : ISelectCustomerRepository
{
    private readonly IDbSession _db;

    public SelectCustomerRepository(IDbSession db) => _db = db;

    public Task<MembershipRecord?> FindMembershipAsync(UserId userId, CustomerId customerId)
        => _db.QuerySingleOrDefaultAsync<MembershipRecord>(
            SqlLoader.Load<SelectCustomerRepository>("FindMembership.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value });

    public async Task<IReadOnlyCollection<ProfileRecord>> GetProfilesAsync(UserId userId, CustomerId customerId)
    {
        var results = await _db.QueryAsync<ProfileRecord>(
            SqlLoader.Load<SelectCustomerRepository>("GetProfiles.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value });
        return results.ToList();
    }

    public Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<SelectCustomerRepository>("SaveRefreshToken.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value, ProfileId = profileId.Value, Token = token, ExpiresAt = expiresAt, LanguageId = languageId });
}

public sealed record MembershipRecord(
    int CustomerId,
    int LanguageId);
