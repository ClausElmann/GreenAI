-- DoesUserHaveRole.sql
-- Returns 1 if the user has the specified global admin role, 0 otherwise.
-- UserRoleMappings has no CustomerId — UserRoles are global across all customers.
-- This is expected by design (FOUNDATIONAL_DOMAIN_ANALYSIS — global role system).
SELECT
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM   [dbo].[UserRoleMappings]   urm
        INNER JOIN [dbo].[UserRoles]      ur  ON ur.[Id] = urm.[UserRoleId]
        WHERE  urm.[UserId]  = @UserId
          AND  ur.[Name]     = @RoleName
    ) THEN 1 ELSE 0 END AS BIT) AS [HasRole];
