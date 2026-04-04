using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.Features.AdminLight.AssignProfile;

public interface IAssignProfileRepository
{
    Task<bool> ProfileBelongsToCustomerAsync(int profileId, CustomerId customerId);
    Task<bool> UserBelongsToCustomerAsync(UserId userId, CustomerId customerId);
    Task AssignProfileAsync(UserId userId, int profileId);
}

public sealed class AssignProfileRepository : IAssignProfileRepository
{
    private readonly IDbSession _db;

    public AssignProfileRepository(IDbSession db) => _db = db;

    public async Task<bool> ProfileBelongsToCustomerAsync(int profileId, CustomerId customerId)
    {
        var count = await _db.QuerySingleOrDefaultAsync<int>(
            SqlLoader.Load<AssignProfileRepository>("FindProfile.sql"),
            new { ProfileId = profileId, CustomerId = customerId.Value });
        return count > 0;
    }

    public async Task<bool> UserBelongsToCustomerAsync(UserId userId, CustomerId customerId)
    {
        var count = await _db.QuerySingleOrDefaultAsync<int>(
            SqlLoader.Load<AssignProfileRepository>("FindUserMembership.sql"),
            new { UserId = userId.Value, CustomerId = customerId.Value });
        return count > 0;
    }

    public Task AssignProfileAsync(UserId userId, int profileId)
        => _db.ExecuteAsync(
            SqlLoader.Load<AssignProfileRepository>("InsertProfileUserMapping.sql"),
            new { UserId = userId.Value, ProfileId = profileId });
}
