using GreenAi.Api.Features.Auth.RefreshToken;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Tests.Features.Auth;

/// <summary>
/// Integration tests for RefreshTokenRepository — verifies the SQL files, not the handler logic.
///
/// Critical behaviour being tested:
///   - FindValidRefreshToken.sql filters on UsedAt IS NULL AND ExpiresAt > now
///   - FindValidRefreshToken.sql reads t.ProfileId directly from UserRefreshTokens (no JOIN/COALESCE)
///   - RevokeRefreshToken.sql sets UsedAt (single-use enforcement)
///   - SaveNewRefreshToken.sql inserts the rotated token with ProfileId
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class RefreshTokenRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public RefreshTokenRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static RefreshTokenRepository CreateRepository() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // FindValidRefreshToken.sql
    // RULE: Returns the token record only when:
    //         UsedAt IS NULL  (not yet consumed)
    //         ExpiresAt > now (not expired)
    //       Also JOINs User + Profile to return UserId, CustomerId,
    //       DefaultProfileId, and Email.
    // ===================================================================

    [Fact]
    public async Task FindValidTokenAsync_FreshUnusedToken_ReturnsRecord()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "bob@example.com" });
        var profileId = await _builder.InsertProfileAsync(customerId, userId);
        await _builder.InsertRefreshTokenAsync(customerId, userId, new()
        {
            Token = "fresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            ProfileId = profileId
        });

        var result = await CreateRepository().FindValidTokenAsync("fresh-token");

        Assert.NotNull(result);
        Assert.Equal(userId.Value, result.UserId);
        Assert.Equal(customerId.Value, result.CustomerId);
        Assert.Equal(profileId, result.ProfileId);
        Assert.Equal("bob@example.com", result.Email);
    }

    [Fact]
    public async Task FindValidTokenAsync_TokenStoredWithProfileId_ReturnsStoredProfileId()
    {
        // TESTING: ProfileId is read directly from t.ProfileId column (no JOIN/COALESCE from Step 11+).
        // V008 DEFAULT 0 covers pre-Step-11 rows; this test validates the column is read correctly.
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "noprofile@example.com" });
        await _builder.InsertRefreshTokenAsync(customerId, userId, new() { Token = "token-no-profile", ProfileId = 0 });

        var result = await CreateRepository().FindValidTokenAsync("token-no-profile");

        Assert.NotNull(result);
        Assert.Equal(0, result.ProfileId); // DEFAULT 0 row from V008 migration path — pass-through only
    }

    [Fact]
    public async Task FindValidTokenAsync_AlreadyUsedToken_ReturnsNull()
    {
        // TESTING: WHERE UsedAt IS NULL — consumed tokens must be rejected
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        await _builder.InsertRefreshTokenAsync(customerId, userId, new()
        {
            Token = "used-token",
            UsedAt = DateTimeOffset.UtcNow.AddMinutes(-5) // already used
        });

        var result = await CreateRepository().FindValidTokenAsync("used-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindValidTokenAsync_ExpiredToken_ReturnsNull()
    {
        // TESTING: WHERE ExpiresAt > @UtcNow — expired tokens must be rejected
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        await _builder.InsertRefreshTokenAsync(customerId, userId, new()
        {
            Token = "expired-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) // expired yesterday
        });

        var result = await CreateRepository().FindValidTokenAsync("expired-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindValidTokenAsync_UnknownToken_ReturnsNull()
    {
        var result = await CreateRepository().FindValidTokenAsync("token-that-does-not-exist");

        Assert.Null(result);
    }

    // ===================================================================
    // RevokeRefreshToken.sql
    // RULE: Sets UsedAt to the provided timestamp for the given token Id.
    //       This makes the token invalid for any future FindValid calls.
    // ===================================================================

    [Fact]
    public async Task RevokeTokenAsync_SetsUsedAt()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var tokenId = await _builder.InsertRefreshTokenAsync(customerId, userId, new()
        {
            Token = "to-be-revoked",
            UsedAt = null // starts as unused
        });

        await CreateRepository().RevokeTokenAsync(tokenId);

        var usedAt = await _builder.ReadTokenUsedAtAsync(tokenId);
        Assert.NotNull(usedAt); // UsedAt must now be set
    }

    [Fact]
    public async Task RevokeTokenAsync_RevokedTokenIsNoLongerValid()
    {
        // TESTING: Full round-trip — revoke then try FindValid → must return null
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "carol@example.com" });
        var tokenId = await _builder.InsertRefreshTokenAsync(customerId, userId, new()
        {
            Token = "single-use-token"
        });

        var repo = CreateRepository();
        await repo.RevokeTokenAsync(tokenId);
        var result = await repo.FindValidTokenAsync("single-use-token");

        Assert.Null(result); // Token cannot be used again after revocation
    }

    // ===================================================================
    // SaveNewRefreshToken.sql
    // RULE: Inserts a new token row — used after rotation to issue
    //       the replacement token.
    // ===================================================================

    [Fact]
    public async Task SaveNewTokenAsync_InsertsTokenAndItIsImmediatelyFindable()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "dave@example.com" });
        var profileId = new ProfileId(await _builder.InsertProfileAsync(customerId, userId));

        var repo = CreateRepository();
        await repo.SaveNewTokenAsync(userId, customerId, profileId, "rotated-token", DateTimeOffset.UtcNow.AddDays(30), languageId: 1);
        var result = await repo.FindValidTokenAsync("rotated-token");

        Assert.NotNull(result);
        Assert.Equal(userId.Value, result.UserId);
        Assert.Equal(profileId.Value, result.ProfileId);
    }

    [Fact]
    public async Task SaveNewTokenAsync_DoesNotAffectOtherTokensForSameUser()
    {
        // TESTING: Rotation adds a NEW row — it does not modify the old token
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var profileId = new ProfileId(await _builder.InsertProfileAsync(customerId, userId));
        await _builder.InsertRefreshTokenAsync(customerId, userId, new() { Token = "original-token", ProfileId = profileId.Value });

        await CreateRepository().SaveNewTokenAsync(userId, customerId, profileId, "new-token", DateTimeOffset.UtcNow.AddDays(30), languageId: 1);

        var count = await _builder.CountRefreshTokensByUserIdAsync(userId);
        Assert.Equal(2, count); // Both tokens must exist (old revoked separately by handler)
    }
}
