-- UpdateUserEmail.sql
UPDATE Users
SET    Email     = @NewEmail,
       RowVersion = NEWID()
WHERE  Id = @UserId;
