-- GetUserProfileAssignments.sql
-- Returns all profiles a given user has access to, scoped to this customer.
-- Tenant-safe: WHERE p.CustomerId = @CustomerId always present.

SELECT
    p.[Id]          AS ProfileId,
    p.[DisplayName] AS ProfileName
FROM [dbo].[ProfileUserMappings] m
JOIN [dbo].[Profiles]            p ON p.[Id]         = m.[ProfileId]
WHERE m.[UserId]     = @UserId
  AND p.[CustomerId] = @CustomerId
ORDER BY p.[DisplayName];
