INSERT INTO [dbo].[Users]
    ([Email], [PasswordHash], [PasswordSalt], [IsActive], [FailedLoginCount], [IsLockedOut])
OUTPUT INSERTED.[Id]
VALUES
    (@Email, @PasswordHash, @PasswordSalt, 1, 0, 0);
