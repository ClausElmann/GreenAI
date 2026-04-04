SELECT TOP 1
    [Id]    AS UserId,
    [Email] AS Email
FROM [dbo].[Users]
WHERE [Email]    = @Email
  AND [IsActive] = 1;
