UPDATE Users
SET FailedLoginCount = 0,
    IsLockedOut      = 0
WHERE Id = @UserId;
