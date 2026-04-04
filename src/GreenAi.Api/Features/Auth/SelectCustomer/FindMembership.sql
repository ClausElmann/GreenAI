-- FindMembership.sql
-- Post-auth tenant selection query: verifies a specific UserCustomerMembership exists and loads
-- the data needed to continue auth (LanguageId). Profile is resolved separately via GetProfiles.sql.
-- UserId comes from the authenticated JWT claim (ICurrentUser), not from the client.
-- CustomerId is the tenant being selected — both columns together identify the single row.
-- This query intentionally uses both UserId AND CustomerId as filters (not a tenant data leak),
-- consistent with the post_auth_tenant_resolution exception rule.
--
-- DefaultProfileId: subquery returns single mapped profile for this user+customer, or 0.
-- V009 dropped Profiles.UserId — access is now via ProfileUserMappings.

SELECT
    m.[CustomerId],
    m.[LanguageId],
    COALESCE(
        (
            SELECT TOP 1 p.[Id]
            FROM [dbo].[ProfileUserMappings] pum
            JOIN [dbo].[Profiles] p
                ON p.[Id] = pum.[ProfileId]
               AND p.[CustomerId] = m.[CustomerId]
            WHERE pum.[UserId] = m.[UserId]
            ORDER BY p.[Id]
        ),
        0
    ) AS [DefaultProfileId]
FROM [dbo].[UserCustomerMemberships] m
WHERE m.[UserId]     = @UserId
  AND m.[CustomerId] = @CustomerId
  AND m.[IsActive]   = 1;
