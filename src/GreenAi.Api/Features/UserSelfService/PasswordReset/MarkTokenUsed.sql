UPDATE [dbo].[PasswordResetTokens]
SET    [UsedAt] = SYSDATETIMEOFFSET()
WHERE  [Id]     = @Id;
