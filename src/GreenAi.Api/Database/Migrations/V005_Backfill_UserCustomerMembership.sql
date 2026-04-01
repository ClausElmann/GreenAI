-- V005 Backfill_UserCustomerMembership
-- Copies existing User→Customer relationships from Users.CustomerId into UserCustomerMembership.
-- Idempotent: WHERE NOT EXISTS guards against duplicate rows on re-run.
-- UIX_UserCustomerMembership_UserId_CustomerId also enforces uniqueness at the DB level.
-- Does NOT modify the Users table.

INSERT INTO [dbo].[UserCustomerMembership] ([UserId], [CustomerId], [LanguageId])
SELECT u.[Id], u.[CustomerId], 1
FROM [dbo].[Users] u
WHERE u.[CustomerId] IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM [dbo].[UserCustomerMembership] m
      WHERE m.[UserId]     = u.[Id]
        AND m.[CustomerId] = u.[CustomerId]
  );
