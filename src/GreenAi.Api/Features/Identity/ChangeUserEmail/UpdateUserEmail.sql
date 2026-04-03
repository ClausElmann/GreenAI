-- UpdateUserEmail.sql
-- RowVersion is ROWVERSION (timestamp) — SQL Server updates it automatically on any row change.
UPDATE Users
SET    Email = @NewEmail
WHERE  Id = @UserId;
