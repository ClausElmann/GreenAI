-- DeleteRefreshTokens.sql
-- Invalidates all refresh tokens for a user by deleting them.
-- Called by LogoutHandler. UserId comes from ICurrentUser (JWT claim).
-- Deleting (rather than marking used) ensures tokens cannot be revived
-- by any future code path.

DELETE FROM [dbo].[UserRefreshTokens]
WHERE [UserId] = @UserId;
