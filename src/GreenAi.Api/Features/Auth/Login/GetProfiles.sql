-- Login/GetProfiles.sql
-- Returns all profiles accessible to the authenticated user for the selected customer.
-- Profile access is governed by ProfileUserMappings (many-to-many: Profile ↔ User).
-- A profile belongs to exactly one Customer — tenant isolation via p.CustomerId = @CustomerId.
-- UserId and CustomerId are resolved from JWT / membership — never from client input.
SELECT
    p.[Id]          AS [ProfileId],
    p.[DisplayName]
FROM [dbo].[Profiles] p
INNER JOIN [dbo].[ProfileUserMappings] m ON m.[ProfileId] = p.[Id]
WHERE m.[UserId]     = @UserId
  AND p.[CustomerId] = @CustomerId
ORDER BY p.[Id];
