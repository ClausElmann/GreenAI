-- FindMembership.sql
-- Post-auth tenant selection query: verifies a specific UserCustomerMembership exists and loads
-- the data needed to continue auth (LanguageId). Profile is resolved separately via GetProfiles.sql.
-- UserId comes from the authenticated JWT claim (ICurrentUser), not from the client.
-- CustomerId is the tenant being selected — both columns together identify the single row.
-- This query intentionally uses both UserId AND CustomerId as filters (not a tenant data leak),
-- consistent with the post_auth_tenant_resolution exception rule.

SELECT
    m.[CustomerId],
    m.[LanguageId]
FROM [dbo].[UserCustomerMembership] m
WHERE m.[UserId]     = @UserId
  AND m.[CustomerId] = @CustomerId
  AND m.[IsActive]   = 1;
