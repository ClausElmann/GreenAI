using Dapper;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Features.Auth;

/// <summary>
/// Inserts minimal valid rows needed by auth repository tests.
/// Each test builds its own data — tests do not share state.
///
/// Hierarchy:  Customer → User → (optional) Profile
///             Customer → User → UserRefreshToken
/// </summary>
public sealed class AuthTestDataBuilder(string connectionString)
{
    // ---------------------------------------------------------------
    // Customer
    // ---------------------------------------------------------------

    /// <summary>Inserts a Customer row and returns its Id.</summary>
    public async Task<CustomerId> InsertCustomerAsync(string name = "Test Customer")
    {
        await using var conn = new SqlConnection(connectionString);
        var id = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Customers (Name) OUTPUT INSERTED.Id VALUES (@Name)",
            new { Name = name });
        return new CustomerId(id);
    }

    // ---------------------------------------------------------------
    // User
    // ---------------------------------------------------------------

    public record InsertUserOptions
    {
        public string Email { get; init; } = "user@example.com";
        public string PasswordHash { get; init; } = "hash";
        public string PasswordSalt { get; init; } = "salt";
        public bool IsActive { get; init; } = true;
        public int FailedLoginCount { get; init; } = 0;
        public bool IsLockedOut { get; init; } = false;
    }

    /// <summary>Inserts a User row and returns its Id.</summary>
    public async Task<UserId> InsertUserAsync(InsertUserOptions? opts = null)
    {
        opts ??= new InsertUserOptions();
        await using var conn = new SqlConnection(connectionString);
        var id = await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Users
                (Email, PasswordHash, PasswordSalt,
                 IsActive, FailedLoginCount, IsLockedOut)
            OUTPUT INSERTED.Id
            VALUES
                (@Email, @PasswordHash, @PasswordSalt,
                 @IsActive, @FailedLoginCount, @IsLockedOut)
            """,
            new
            {
                opts.Email,
                opts.PasswordHash,
                opts.PasswordSalt,
                IsActive = opts.IsActive ? 1 : 0,
                opts.FailedLoginCount,
                IsLockedOut = opts.IsLockedOut ? 1 : 0
            });
        return new UserId(id);
    }

    // ---------------------------------------------------------------
    // Profile
    // ---------------------------------------------------------------

    /// <summary>
    /// Inserts a Profile row (no UserId — V009 dropped that column) and grants access
    /// to <paramref name="userId"/> via ProfileUserMappings. Returns the new Profile Id.
    /// </summary>
    public async Task<int> InsertProfileAsync(CustomerId customerId, UserId userId, string displayName = "Test Profile")
    {
        await using var conn = new SqlConnection(connectionString);
        var profileId = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Profiles (CustomerId, DisplayName)
            OUTPUT INSERTED.Id
            VALUES (@CustomerId, @DisplayName)
            """,
            new { CustomerId = customerId.Value, DisplayName = displayName });

        await conn.ExecuteAsync(
            "INSERT INTO ProfileUserMappings (ProfileId, UserId) VALUES (@ProfileId, @UserId)",
            new { ProfileId = profileId, UserId = userId.Value });

        return profileId;
    }

    // ---------------------------------------------------------------
    // Refresh tokens
    // ---------------------------------------------------------------

    public record InsertTokenOptions
    {
        public string Token { get; init; } = "some-token";
        public DateTimeOffset ExpiresAt { get; init; } = DateTimeOffset.UtcNow.AddDays(30);
        public DateTimeOffset? UsedAt { get; init; } = null;
        public int LanguageId { get; init; } = 1;
        public int ProfileId { get; init; } = 0;
    }

    /// <summary>Inserts a UserRefreshToken row and returns its Id.</summary>
    public async Task<int> InsertRefreshTokenAsync(CustomerId customerId, UserId userId, InsertTokenOptions? opts = null)
    {
        opts ??= new InsertTokenOptions();
        await using var conn = new SqlConnection(connectionString);
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO UserRefreshTokens (CustomerId, UserId, Token, ExpiresAt, UsedAt, LanguageId, ProfileId)
            OUTPUT INSERTED.Id
            VALUES (@CustomerId, @UserId, @Token, @ExpiresAt, @UsedAt, @LanguageId, @ProfileId)
            """,
            new
            {
                CustomerId = customerId.Value,
                UserId = userId.Value,
                opts.Token,
                opts.ExpiresAt,
                opts.UsedAt,
                opts.LanguageId,
                opts.ProfileId
            });
    }

    // ---------------------------------------------------------------
    // UserCustomerMembership
    // ---------------------------------------------------------------

    // NOTE:
    // LanguageId defaults to 1 for test convenience only.
    // Production code MUST always provide explicit LanguageId from membership.
    /// <summary>Inserts a UserCustomerMembership row linking a user to a customer.</summary>
    public async Task InsertUserCustomerMembershipAsync(UserId userId, CustomerId customerId, int languageId = 1)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.ExecuteAsync("""
            INSERT INTO UserCustomerMemberships (UserId, CustomerId, LanguageId, IsActive)
            VALUES (@UserId, @CustomerId, @LanguageId, 1)
            """,
            new { UserId = userId.Value, CustomerId = customerId.Value, LanguageId = languageId });
    }

    // ---------------------------------------------------------------
    // Read-back helpers (verify DB state after a repository call)
    // ---------------------------------------------------------------

    public async Task<(int FailedLoginCount, bool IsLockedOut)> ReadUserAuthStateAsync(UserId userId)
    {
        await using var conn = new SqlConnection(connectionString);
        return await conn.QuerySingleAsync<(int, bool)>(
            "SELECT FailedLoginCount, IsLockedOut FROM Users WHERE Id = @UserId",
            new { UserId = userId.Value });
    }

    public async Task<DateTimeOffset?> ReadTokenUsedAtAsync(int tokenId)
    {
        await using var conn = new SqlConnection(connectionString);
        return await conn.QuerySingleAsync<DateTimeOffset?>(
            "SELECT UsedAt FROM UserRefreshTokens WHERE Id = @TokenId",
            new { TokenId = tokenId });
    }

    public async Task<int> CountRefreshTokensByUserIdAsync(UserId userId)
    {
        await using var conn = new SqlConnection(connectionString);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM UserRefreshTokens WHERE UserId = @UserId",
            new { UserId = userId.Value });
    }

    // ---------------------------------------------------------------
    // Role assignments
    // ---------------------------------------------------------------

    /// <summary>Assigns a global UserRole to a user via UserRoleMappings.</summary>
    public async Task AssignUserRoleAsync(UserId userId, string roleName)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.ExecuteAsync("""
            INSERT INTO UserRoleMappings (UserId, UserRoleId)
            SELECT @UserId, Id FROM UserRoles WHERE Name = @RoleName
            """,
            new { UserId = userId.Value, RoleName = roleName });
    }

    /// <summary>Assigns a ProfileRole to a profile via ProfileRoleMappings.</summary>
    public async Task AssignProfileRoleAsync(int profileId, string roleName)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.ExecuteAsync("""
            INSERT INTO ProfileRoleMappings (ProfileId, ProfileRoleId)
            SELECT @ProfileId, Id FROM ProfileRoles WHERE Name = @RoleName
            """,
            new { ProfileId = profileId, RoleName = roleName });
    }

    /// <summary>Inserts a ProfileUserMapping (many-to-many profile access for a user).</summary>
    public async Task InsertProfileUserMappingAsync(UserId userId, int profileId)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.ExecuteAsync(
            "INSERT INTO ProfileUserMappings (ProfileId, UserId) VALUES (@ProfileId, @UserId)",
            new { ProfileId = profileId, UserId = userId.Value });
    }
}
