UPDATE Users
SET FailedLoginCount  = FailedLoginCount + 1,
    LastFailedLoginAt = @UtcNow,
    IsLockedOut       = CASE WHEN FailedLoginCount + 1 >= 10 THEN 1 ELSE 0 END
WHERE Id = @UserId;
