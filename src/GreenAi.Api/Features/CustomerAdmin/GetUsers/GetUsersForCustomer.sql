-- GetUsersForCustomer.sql
-- Returns all users who have a membership for this customer.
-- Tenant-safe: joined through UserCustomerMemberships on CustomerId = @CustomerId.

SELECT
    u.[Id],
    u.[Email],
    u.[IsActive]
FROM [dbo].[Users]                   u
JOIN [dbo].[UserCustomerMemberships] m ON m.[UserId]     = u.[Id]
                                      AND m.[CustomerId] = @CustomerId
WHERE u.[IsActive] = 1
ORDER BY u.[Email];
