using Dapper;
using GreenAi.Api.Features.Identity.ChangeUserEmail;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Features.Auth;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Features.Identity;

/// <summary>
/// Integration tests for ChangeUserEmailRepository — verifies the SQL files, not handler logic.
///
/// Each test:
///   1. Seeds its own data via AuthTestDataBuilder
///   2. Calls repository methods
///   3. Asserts the DB state or return value
///
/// Respawn resets all data between tests so tests are fully independent.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ChangeUserEmailRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AuthTestDataBuilder _builder;

    public ChangeUserEmailRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new AuthTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static ChangeUserEmailRepository CreateRepository() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    // ===================================================================
    // CheckEmailAvailable.sql
    // RULE: Returns 0 if the email is not taken by another user (available).
    //       Returns >0 if another user already owns that email (conflict).
    //       Own email (excludeUserId) does NOT count as a conflict.
    // ===================================================================

    [Fact]
    public async Task IsEmailAvailableAsync_EmailNotTaken_ReturnsTrue()
    {
        // Arrange — no other user with that email
        var userId = await _builder.InsertUserAsync(new() { Email = "current@example.com" });

        // Act
        var available = await CreateRepository().IsEmailAvailableAsync("new@example.com", userId);

        // Assert
        Assert.True(available);
    }

    [Fact]
    public async Task IsEmailAvailableAsync_EmailTakenByOtherUser_ReturnsFalse()
    {
        // Arrange — another user has the email we want
        await _builder.InsertUserAsync(new() { Email = "taken@example.com" });
        var currentUserId = await _builder.InsertUserAsync(new() { Email = "current@example.com" });

        // Act
        var available = await CreateRepository().IsEmailAvailableAsync("taken@example.com", currentUserId);

        // Assert
        Assert.False(available);
    }

    [Fact]
    public async Task IsEmailAvailableAsync_OwnCurrentEmail_ReturnsTrue()
    {
        // Arrange — user requesting same email they already have (no conflict with self)
        var userId = await _builder.InsertUserAsync(new() { Email = "same@example.com" });

        // Act — excludeUserId = this user, so own email should report as available
        var available = await CreateRepository().IsEmailAvailableAsync("same@example.com", userId);

        // Assert
        Assert.True(available);
    }

    // ===================================================================
    // UpdateUserEmail.sql + InsertAuditEntry.sql (transactional)
    // RULE: Both operations run atomically — email update AND audit entry
    //       must both commit or both rollback.
    // ===================================================================

    [Fact]
    public async Task UpdateEmailAndAuditAsync_UpdatesEmailInUsersTable()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "old@example.com" });

        // Act
        await CreateRepository().UpdateEmailAndAuditAsync(userId, customerId, "new@example.com");

        // Assert — Users.Email updated
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var email = await conn.ExecuteScalarAsync<string>(
            "SELECT Email FROM Users WHERE Id = @Id", new { Id = userId.Value });
        Assert.Equal("new@example.com", email);
    }

    [Fact]
    public async Task UpdateEmailAndAuditAsync_WritesAuditEntry()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync();
        var userId = await _builder.InsertUserAsync(new() { Email = "old@example.com" });

        // Act
        await CreateRepository().UpdateEmailAndAuditAsync(userId, customerId, "audited@example.com");

        // Assert — AuditLog row exists with correct metadata
        await using var conn = new SqlConnection(DatabaseFixture.ConnectionString);
        var entry = await conn.QuerySingleOrDefaultAsync(
            """
            SELECT CustomerId, UserId, ActorId, Action, Details
            FROM   AuditLog
            WHERE  UserId = @UserId
            """,
            new { UserId = userId.Value });

        Assert.NotNull(entry);
        Assert.Equal(customerId.Value, (int)entry!.CustomerId);
        Assert.Equal(userId.Value,     (int)entry.UserId);
        Assert.Equal(userId.Value,     (int)entry.ActorId);
        Assert.Equal("EMAIL_CHANGED",  (string)entry.Action);
        Assert.Contains("audited@example.com", (string)entry.Details);
    }
}
