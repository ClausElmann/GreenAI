-- Verify target user is a member of the caller's customer
SELECT COUNT(1)
FROM   [dbo].[UserCustomerMemberships]
WHERE  [UserId]     = @UserId
  AND  [CustomerId] = @CustomerId
  AND  [IsActive]   = 1;
