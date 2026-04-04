SELECT TOP 1
    [Id]        AS Id,
    [UserId]    AS UserId,
    [Token]     AS Token,
    [ExpiresAt] AS ExpiresAt
FROM [dbo].[PasswordResetTokens]
WHERE [Token]    = @Token
  AND [UsedAt]   IS NULL
  AND [ExpiresAt] > SYSDATETIMEOFFSET();
