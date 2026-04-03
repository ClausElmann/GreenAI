using Dapper;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Data.SqlClient;

namespace GreenAi.Tests.Features.CustomerAdmin;

/// <summary>
/// Inserts minimal valid rows needed by CustomerAdmin handler/repository tests.
/// Reuses AuthTestDataBuilder for user/customer/profile seeding — this builder adds
/// customer-admin-specific read-back helpers.
/// </summary>
public sealed class CustomerAdminTestDataBuilder(string connectionString)
{
    private readonly GreenAi.Tests.Features.Auth.AuthTestDataBuilder _auth = new(connectionString);

    // ---------------------------------------------------------------
    // Delegation to AuthTestDataBuilder
    // ---------------------------------------------------------------

    public Task<CustomerId> InsertCustomerAsync(string name = "Test Customer") =>
        _auth.InsertCustomerAsync(name);

    public Task<UserId> InsertUserAsync(Auth.AuthTestDataBuilder.InsertUserOptions? opts = null) =>
        _auth.InsertUserAsync(opts);

    public Task<int> InsertProfileAsync(CustomerId customerId, UserId userId, string displayName = "Test Profile") =>
        _auth.InsertProfileAsync(customerId, userId, displayName);

    public Task InsertUserCustomerMembershipAsync(UserId userId, CustomerId customerId, int languageId = 1) =>
        _auth.InsertUserCustomerMembershipAsync(userId, customerId, languageId);

    // ---------------------------------------------------------------
    // Read-back helpers for CustomerAdmin assertions
    // ---------------------------------------------------------------

    public async Task<int> CountProfilesForCustomerAsync(CustomerId customerId)
    {
        await using var conn = new SqlConnection(connectionString);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Profiles WHERE CustomerId = @CustomerId",
            new { CustomerId = customerId.Value });
    }

    public async Task<int> CountUsersForCustomerAsync(CustomerId customerId)
    {
        await using var conn = new SqlConnection(connectionString);
        return await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(DISTINCT u.Id)
            FROM Users u
            JOIN UserCustomerMemberships ucm ON ucm.UserId = u.Id
            WHERE ucm.CustomerId = @CustomerId AND u.IsActive = 1
            """,
            new { CustomerId = customerId.Value });
    }
}
