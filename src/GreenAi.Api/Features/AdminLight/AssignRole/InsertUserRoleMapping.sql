INSERT INTO [dbo].[UserRoleMappings] ([UserId], [UserRoleId])
SELECT @UserId, [Id]
FROM   [dbo].[UserRoles]
WHERE  [Name] = @RoleName
  AND  NOT EXISTS (
      SELECT 1
      FROM   [dbo].[UserRoleMappings]
      WHERE  [UserId]     = @UserId
        AND  [UserRoleId] = (SELECT [Id] FROM [dbo].[UserRoles] WHERE [Name] = @RoleName)
  );
