-- CanUserAccessCustomer.sql
-- Returns 1 if the user has an active membership for the given customer, 0 otherwise.
-- Used by CanUserAccessCustomerAsync to gate tenant-scoped operations.
-- UserId + CustomerId together identify the unique membership row.
SELECT
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM   [dbo].[UserCustomerMemberships]
        WHERE  [UserId]     = @UserId
          AND  [CustomerId] = @CustomerId
          AND  [IsActive]   = 1
    ) THEN 1 ELSE 0 END AS BIT) AS [CanAccess];
