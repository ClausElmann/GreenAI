-- GetUserRoleAssignments.sql
-- Returns all user-role assignments for a given user.
-- UserRoleMappings is global (no CustomerId column by design — see V010 comments).

SELECT
    r.[Name] AS Role,
    ''       AS ProfileName
FROM [dbo].[UserRoleMappings] m
JOIN [dbo].[UserRoles]        r ON r.[Id] = m.[UserRoleId]
WHERE m.[UserId] = @UserId
ORDER BY r.[Name];
