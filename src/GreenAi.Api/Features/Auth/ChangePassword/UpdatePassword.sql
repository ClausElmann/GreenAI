UPDATE [dbo].[Users]
SET
    PasswordHash = @PasswordHash,
    PasswordSalt = @PasswordSalt
WHERE Id = @UserId;
