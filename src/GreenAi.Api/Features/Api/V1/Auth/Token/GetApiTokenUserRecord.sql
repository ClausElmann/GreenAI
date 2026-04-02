-- GetApiTokenUserRecord.sql
-- Loads user credentials + verifies API role + customer membership + profile access
-- in a single query. Returns NULL if any condition is not met.

SELECT
    u.[Id],
    u.[Email],
    u.[PasswordHash],
    u.[PasswordSalt],
    u.[IsLockedOut],
    u.[FailedLoginCount],
    ucm.[LanguageId]
FROM [dbo].[Users]                      u
INNER JOIN [dbo].[UserRoleMappings]     urm ON urm.[UserId] = u.[Id]
INNER JOIN [dbo].[UserRoles]            ur  ON ur.[Id]      = urm.[UserRoleId]
                                          AND ur.[Name]     = 'API'
INNER JOIN [dbo].[UserCustomerMemberships] ucm ON ucm.[UserId]     = u.[Id]
                                               AND ucm.[CustomerId] = @CustomerId
INNER JOIN [dbo].[ProfileUserMappings]  pum ON pum.[UserId]     = u.[Id]
                                           AND pum.[ProfileId]  = @ProfileId
INNER JOIN [dbo].[Profiles]             p   ON p.[Id]          = pum.[ProfileId]
                                           AND p.[CustomerId]   = @CustomerId
WHERE u.[Email] = @Email;
