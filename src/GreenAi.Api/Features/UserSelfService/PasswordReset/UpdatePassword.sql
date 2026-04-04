UPDATE [dbo].[Users]
SET    [PasswordHash]      = @PasswordHash,
       [PasswordSalt]      = @PasswordSalt,
       [FailedLoginCount]  = 0
WHERE  [Id] = @UserId;
