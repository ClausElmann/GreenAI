using GreenAi.Api.Features.Auth.ChangePassword;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Tests.Database;

namespace GreenAi.Tests.Features.Auth;

/// <summary>
/// Integration tests for ChangePasswordRepository — verifies the SQL files, not handler logic.
///
/// Each test:
///   1. Seeds its own data via AuthTestDataBuilder
///   2. Calls ONE repository method
///   3. Asserts the DB state or return value
///
/// Respawn resets all data between tests so tests are fully independent.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ChangePasswordRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public ChangePasswordRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static ChangePasswordRepository CreateRepository() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // GetUserCredentials.sql
    // RULE: Returns credentials for an active, non-NULL user by UserId.
    //       Returns NULL for inactive or unknown users.
    // ===================================================================

    [Fact]
    public async Task FindByUserIdAsync_ActiveUser_ReturnsCredentialRecord()
    {
        // Arrange
        var userId = await _builder.InsertUserAsync(new()
        {
            PasswordHash = "stored-hash",
            PasswordSalt = "stored-salt",
            IsActive = true,
            IsLockedOut = false
        });

        // Act
        var result = await CreateRepository().FindByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId.Value, result.Id);
        Assert.Equal("stored-hash", result.PasswordHash);
        Assert.Equal("stored-salt", result.PasswordSalt);
        Assert.False(result.IsLockedOut);
    }

    [Fact]
    public async Task FindByUserIdAsync_LockedOutUser_ReturnsRecordWithIsLockedOutTrue()
    {
        // Arrange — locked out but active → still returned so handler can emit ACCOUNT_LOCKED
        var userId = await _builder.InsertUserAsync(new()
        {
            IsActive = true,
            IsLockedOut = true
        });

        // Act
        var result = await CreateRepository().FindByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsLockedOut);
    }

    [Fact]
    public async Task FindByUserIdAsync_InactiveUser_ReturnsNull()
    {
        // Arrange — IsActive = false means account disabled
        var userId = await _builder.InsertUserAsync(new()
        {
            IsActive = false
        });

        // Act
        var result = await CreateRepository().FindByUserIdAsync(userId);

        // Assert — SQL filters IsActive = 1
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByUserIdAsync_UnknownUserId_ReturnsNull()
    {
        // Arrange — no user seeded
        var unknownUserId = new GreenAi.Api.SharedKernel.Ids.UserId(99999);

        // Act
        var result = await CreateRepository().FindByUserIdAsync(unknownUserId);

        // Assert
        Assert.Null(result);
    }

    // ===================================================================
    // UpdatePassword.sql
    // RULE: Updates PasswordHash + PasswordSalt for the given UserId.
    // ===================================================================

    [Fact]
    public async Task UpdatePasswordAsync_ExistingUser_PersistsNewHashAndSalt()
    {
        // Arrange
        var userId = await _builder.InsertUserAsync(new()
        {
            PasswordHash = "old-hash",
            PasswordSalt = "old-salt",
            IsActive = true
        });
        var repo = CreateRepository();

        // Act
        await repo.UpdatePasswordAsync(userId, "new-hash", "new-salt");

        // Assert — read back via FindByUserIdAsync to confirm DB state changed
        var updated = await repo.FindByUserIdAsync(userId);
        Assert.NotNull(updated);
        Assert.Equal("new-hash", updated.PasswordHash);
        Assert.Equal("new-salt", updated.PasswordSalt);
    }
}
