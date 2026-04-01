-- GetAvailableProfiles.sql
-- Returns all profiles accessible to the authenticated user for the active customer.
-- Profile access is governed by ProfileUserMappings (many-to-many: Profile ↔ User).
-- A profile belongs to exactly one Customer — tenant isolation via p.CustomerId = @CustomerId.
-- UserId and CustomerId come from the authenticated JWT (ICurrentUser) — never from client.
-- All returned rows have Id > 0 (real Profiles table rows).
SELECT
    p.[Id]          AS [ProfileId],
    p.[DisplayName]
FROM [dbo].[Profiles] p
INNER JOIN [dbo].[ProfileUserMappings] m ON m.[ProfileId] = p.[Id]
WHERE m.[UserId]     = @UserId
  AND p.[CustomerId] = @CustomerId
ORDER BY p.[Id];
