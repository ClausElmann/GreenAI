using Dapper;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.Auth.SelectProfile;

public sealed class SelectProfileRepository : ISelectProfileRepository
{
    private readonly IDbSession _db;

    public SelectProfileRepository(IDbSession db) => _db = db;

    public async Task<IReadOnlyCollection<ProfileRecord>> GetAvailableProfilesAsync(UserId userId, CustomerId customerId)
    {
        var results = await _db.QueryAsync<ProfileRecord>(
            SqlLoader.Load<SelectProfileRepository>("GetAvailableProfiles.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value });
        return results.ToList();
    }

    public Task SaveRefreshTokenAsync(UserId userId, CustomerId customerId, ProfileId profileId, string token, DateTimeOffset expiresAt, int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<SelectProfileRepository>("SaveRefreshToken.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value, ProfileId = profileId.Value, Token = token, ExpiresAt = expiresAt, LanguageId = languageId });
}
