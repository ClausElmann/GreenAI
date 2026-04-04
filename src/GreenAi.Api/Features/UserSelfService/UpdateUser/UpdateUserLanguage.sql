UPDATE [dbo].[UserCustomerMemberships]
SET    [LanguageId] = @LanguageId
WHERE  [UserId]     = @UserId
  AND  [CustomerId] = @CustomerId;
