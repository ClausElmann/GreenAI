-- DoesProfileHaveRole.sql
-- Returns 1 if the profile has the specified operational capability flag, 0 otherwise.
-- ProfileRoleMappings is the PRIMARY feature gate for all send/operational capabilities.
-- Called by DoesProfileHaveRoleAsync — the authority for all capability checks.
SELECT
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM   [dbo].[ProfileRoleMappings]  prm
        INNER JOIN [dbo].[ProfileRoles]     pr  ON pr.[Id] = prm.[ProfileRoleId]
        WHERE  prm.[ProfileId] = @ProfileId
          AND  pr.[Name]       = @RoleName
    ) THEN 1 ELSE 0 END AS BIT) AS [HasRole];
