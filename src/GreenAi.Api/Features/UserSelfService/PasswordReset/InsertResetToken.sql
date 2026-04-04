INSERT INTO [dbo].[PasswordResetTokens] ([UserId], [Token], [ExpiresAt])
VALUES (@UserId, @Token, @ExpiresAt);
