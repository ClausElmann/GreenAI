-- GetCustomerSettings.sql
-- Returns the editable settings for the current customer.
-- Tenant-safe: WHERE CustomerId = @CustomerId always present.

SELECT
    c.[Id],
    c.[Name]
FROM [dbo].[Customers] c
WHERE c.[Id] = @CustomerId;
