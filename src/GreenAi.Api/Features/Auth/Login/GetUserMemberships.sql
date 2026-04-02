-- GetUserMemberships.sql
-- Post-auth tenant resolution query: returns all active Customer memberships for a given User.
-- This query intentionally omits WHERE CustomerId because its purpose IS to discover
-- which Customers the user belongs to (post-auth resolution, before a scoped JWT is issued).
-- Exception documented in 05_EXECUTION_RULES.json#post_auth_tenant_resolution.

SELECT
    m.[CustomerId],
    c.[Name]  AS [CustomerName],
    m.[LanguageId]
FROM [dbo].[UserCustomerMemberships] m
INNER JOIN [dbo].[Customers] c ON c.[Id] = m.[CustomerId]
WHERE m.[UserId]    = @UserId
  AND m.[IsActive]  = 1;
