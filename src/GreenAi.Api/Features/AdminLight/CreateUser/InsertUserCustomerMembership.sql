INSERT INTO [dbo].[UserCustomerMemberships]
    ([UserId], [CustomerId], [LanguageId], [IsActive])
VALUES
    (@UserId, @CustomerId, @LanguageId, 1);
