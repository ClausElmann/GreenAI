using GreenAi.Api.Features.Auth.Login;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Tests.Features.Auth;

/// <summary>
/// Integration tests for LoginRepository — verifies the SQL files, not the handler logic.
///
/// Each test:
///   1. Seeds its own data via AuthTestDataBuilder
///   2. Calls ONE repository method
///   3. Asserts the DB state or return value
///
/// Respawn resets all data between tests so tests are fully independent.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class LoginRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public LoginRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static LoginRepository CreateRepository() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // FindUserByEmail.sql
    // RULE: Returns the user row (+ profile) for an active user by email.
    //       Returns NULL for inactive users or unknown emails.
    // ===================================================================

    [Fact]
    public async Task FindByEmailAsync_ActiveUser_ReturnsIdentityFields()
    {
        // Arrange
        var userId = await _builder.InsertUserAsync(new()
        {
            Email = "alice@example.com",
            PasswordHash = "hash",
            PasswordSalt = "salt"
        });

        // Act
        var result = await CreateRepository().FindByEmailAsync("alice@example.com");

        // Assert — identity fields only: no CustomerId, no ProfileId
        Assert.NotNull(result);
        Assert.Equal(userId.Value, result.Id);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("hash", result.PasswordHash);
        Assert.Equal("salt", result.PasswordSalt);
    }

    [Fact]
    public async Task FindByEmailAsync_InactiveUser_ReturnsNull()
    {
        // TESTING: WHERE IsActive = 1 — deactivated users cannot log in
        await _builder.InsertUserAsync(new()
        {
            Email = "inactive@example.com",
            IsActive = false
        });

        var result = await CreateRepository().FindByEmailAsync("inactive@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var result = await CreateRepository().FindByEmailAsync("nobody@example.com");

        Assert.Null(result);
    }

    // ===================================================================
    // RecordFailedLogin.sql
    // RULE: Increments FailedLoginCount by 1 on each call.
    //       Locks the account when FailedLoginCount reaches EXACTLY 10.
    // ===================================================================

    [Fact]
    public async Task RecordFailedLoginAsync_FirstFailure_IncrementsCountToOne()
    {
        var userId = await _builder.InsertUserAsync(new() { FailedLoginCount = 0 });

        await CreateRepository().RecordFailedLoginAsync(userId);

        var (count, locked) = await _builder.ReadUserAuthStateAsync(userId);
        Assert.Equal(1, count);
        Assert.False(locked);
    }

    [Fact]
    public async Task RecordFailedLoginAsync_NinthFailure_CountIsNineAndNotLocked()
    {
        // TESTING: Boundary — the 9th failure must NOT lock the account (10 is the threshold)
        var userId = await _builder.InsertUserAsync(new() { FailedLoginCount = 8 });

        await CreateRepository().RecordFailedLoginAsync(userId);

        var (count, locked) = await _builder.ReadUserAuthStateAsync(userId);
        Assert.Equal(9, count);
        Assert.False(locked); // Not yet locked — must reach 10
    }

    [Fact]
    public async Task RecordFailedLoginAsync_TenthFailure_LocksAccount()
    {
        // TESTING: Boundary — the 10th failure MUST lock the account
        var userId = await _builder.InsertUserAsync(new() { FailedLoginCount = 9 });

        await CreateRepository().RecordFailedLoginAsync(userId);

        var (count, locked) = await _builder.ReadUserAuthStateAsync(userId);
        Assert.Equal(10, count);
        Assert.True(locked); // 9 + 1 = 10 >= 10 → locked
    }

    // ===================================================================
    // ResetFailedLogin.sql
    // RULE: Resets both FailedLoginCount to 0 and IsLockedOut to false.
    // ===================================================================

    [Fact]
    public async Task ResetFailedLoginAsync_ResetsCountAndUnlocksAccount()
    {
        // TESTING: Both fields must be reset — not just one
        var userId = await _builder.InsertUserAsync(new()
        {
            FailedLoginCount = 10,
            IsLockedOut = true
        });

        await CreateRepository().ResetFailedLoginAsync(userId);

        var (count, locked) = await _builder.ReadUserAuthStateAsync(userId);
        Assert.Equal(0, count);
        Assert.False(locked);
    }

    // ===================================================================
    // SaveRefreshToken.sql
    // RULE: Inserts a new token row linked to the user and customer.
    // ===================================================================

    [Fact]
    public async Task SaveRefreshTokenAsync_InsertsTokenRow()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync();
        var rawProfileId = await _builder.InsertProfileAsync(customerId, userId);
        var profileId = new ProfileId(rawProfileId);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        await CreateRepository().SaveRefreshTokenAsync(userId, customerId, profileId, "my-token-value", expiresAt, languageId: 1);

        var count = await _builder.CountRefreshTokensByUserIdAsync(userId);
        Assert.Equal(1, count);
    }
}
