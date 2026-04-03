-- GetProfilesForCustomer.sql
-- Returns all profiles belonging to this customer.
-- Tenant-safe: WHERE CustomerId = @CustomerId always present.

SELECT
    p.[Id],
    p.[DisplayName] AS Name,
    CAST(1 AS BIT)  AS IsActive
FROM [dbo].[Profiles] p
WHERE p.[CustomerId] = @CustomerId
ORDER BY p.[DisplayName];
