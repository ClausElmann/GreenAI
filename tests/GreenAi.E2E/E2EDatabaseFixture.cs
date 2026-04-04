using Microsoft.Data.SqlClient;

namespace GreenAi.E2E;

/// <summary>
/// Ensures dev seed data (admin@dev.local, sender@dev.local, Testkommune, profiles)
/// exists in the DB before E2E tests run.
/// Safe to run even if data already exists (idempotent upsert via MERGE).
/// </summary>
public sealed class E2EDatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Integrated Security=true;TrustServerCertificate=true;";

    public async ValueTask InitializeAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Ensure Testkommune exists
        await ExecAsync(conn, """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Name] = 'Testkommune')
                INSERT INTO [dbo].[Customers] ([Name]) VALUES ('Testkommune');
            """);

        var customerId = await ScalarAsync<int>(conn,
            "SELECT [Id] FROM [dbo].[Customers] WHERE [Name] = 'Testkommune'");

        // Ensure users exist (no CustomerId column since V007 removed it)
        await ExecAsync(conn, """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'admin@dev.local')
                INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [PasswordSalt], [IsActive])
                VALUES (
                    'admin@dev.local',
                    'jX4HKkXh60GLPYnd5RBku7PVbsF6Oijuerfd4/zV6LeF2QpsvE9ysH7Jyl0kRY7IJR0ctlaseEh64iuCdQVKfw==',
                    'fLKlYkEcWwpdIAH+Wkx1CXFg1CD0c/rMiKc/RFTmyqg=',
                    1);
            """);

        await ExecAsync(conn, """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'sender@dev.local')
                INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [PasswordSalt], [IsActive])
                VALUES (
                    'sender@dev.local',
                    'jX4HKkXh60GLPYnd5RBku7PVbsF6Oijuerfd4/zV6LeF2QpsvE9ysH7Jyl0kRY7IJR0ctlaseEh64iuCdQVKfw==',
                    'fLKlYkEcWwpdIAH+Wkx1CXFg1CD0c/rMiKc/RFTmyqg=',
                    1);
            """);

        var adminId  = await ScalarAsync<int>(conn, "SELECT [Id] FROM [dbo].[Users] WHERE [Email] = 'admin@dev.local'");
        var senderId = await ScalarAsync<int>(conn, "SELECT [Id] FROM [dbo].[Users] WHERE [Email] = 'sender@dev.local'");

        // UserCustomerMemberships
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {adminId} AND [CustomerId] = {customerId})
                INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId]) VALUES ({adminId}, {customerId}, 1);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = {senderId} AND [CustomerId] = {customerId})
                INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId]) VALUES ({senderId}, {customerId}, 1);
            """);

        // Profiles
        await ExecAsync(conn, $"""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Profiles] WHERE [CustomerId] = {customerId} AND [DisplayName] = 'Nordjylland')
                INSERT INTO [dbo].[Profiles] ([CustomerId], [DisplayName]) VALUES ({customerId}, 'Nordjylland');
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Profiles] WHERE [CustomerId] = {customerId} AND [DisplayName] = 'Sønderjylland')
                INSERT INTO [dbo].[Profiles] ([CustomerId], [DisplayName]) VALUES ({customerId}, 'Sønderjylland');
            """);

        var profile1Id = await ScalarAsync<int>(conn, $"SELECT [Id] FROM [dbo].[Profiles] WHERE [CustomerId] = {customerId} AND [DisplayName] = 'Nordjylland'");
        var profile2Id = await ScalarAsync<int>(conn, $"SELECT [Id] FROM [dbo].[Profiles] WHERE [CustomerId] = {customerId} AND [DisplayName] = 'Sønderjylland'");

        // ProfileUserMappings — admin gets Nordjylland only (1 profile → login auto-resolves full JWT).
        // Sønderjylland mapping for admin is intentionally removed: admin having 2 profiles triggers
        // RequiresProfileSelection in LoginHandler which the login UI doesn't handle yet.
        // The CustomerAdmin profiles list still shows both profiles (query is by CustomerId, not UserId).
        await ExecAsync(conn, $"""
            DELETE FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profile2Id} AND [UserId] = {adminId};
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profile1Id} AND [UserId] = {adminId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profile1Id}, {adminId});
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [ProfileId] = {profile1Id} AND [UserId] = {senderId})
                INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId]) VALUES ({profile1Id}, {senderId});
            """);

        // UserRoleMappings — admin@dev.local gets SuperAdmin + other roles for full E2E coverage.
        // Note: V015 migration may not have inserted these if UserRoles were seeded in a later migration.
        // This fixture ensures roles are always present for E2E tests regardless of migration order.
        await ExecAsync(conn, $"""
            DECLARE @RoleSuperAdmin    INT = (SELECT [Id] FROM [dbo].[UserRoles] WHERE [Name] = 'SuperAdmin');
            DECLARE @RoleManageUsers   INT = (SELECT [Id] FROM [dbo].[UserRoles] WHERE [Name] = 'ManageUsers');
            DECLARE @RoleManageProfiles INT = (SELECT [Id] FROM [dbo].[UserRoles] WHERE [Name] = 'ManageProfiles');
            DECLARE @RoleCustomerSetup  INT = (SELECT [Id] FROM [dbo].[UserRoles] WHERE [Name] = 'CustomerSetup');
            IF @RoleSuperAdmin IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleMappings] WHERE [UserId] = {adminId} AND [UserRoleId] = @RoleSuperAdmin)
                INSERT INTO [dbo].[UserRoleMappings] ([UserId], [UserRoleId]) VALUES ({adminId}, @RoleSuperAdmin);
            IF @RoleManageUsers IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleMappings] WHERE [UserId] = {adminId} AND [UserRoleId] = @RoleManageUsers)
                INSERT INTO [dbo].[UserRoleMappings] ([UserId], [UserRoleId]) VALUES ({adminId}, @RoleManageUsers);
            IF @RoleManageProfiles IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleMappings] WHERE [UserId] = {adminId} AND [UserRoleId] = @RoleManageProfiles)
                INSERT INTO [dbo].[UserRoleMappings] ([UserId], [UserRoleId]) VALUES ({adminId}, @RoleManageProfiles);
            IF @RoleCustomerSetup IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleMappings] WHERE [UserId] = {adminId} AND [UserRoleId] = @RoleCustomerSetup)
                INSERT INTO [dbo].[UserRoleMappings] ([UserId], [UserRoleId]) VALUES ({adminId}, @RoleCustomerSetup);
            """);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task ExecAsync(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }
}

[CollectionDefinition("E2E")]
public sealed class E2ECollection : ICollectionFixture<E2EDatabaseFixture>;
