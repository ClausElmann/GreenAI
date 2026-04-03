SELECT
    Id,
    PasswordHash,
    PasswordSalt,
    IsLockedOut
FROM [dbo].[Users]
WHERE Id = @UserId
  AND IsActive = 1;
