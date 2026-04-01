CREATE TABLE [dbo].[UserRoleMappings] (
    [UserId]      INT  NOT NULL,
    [UserRoleId]  INT  NOT NULL,
    CONSTRAINT [PK_UserRoleMappings] PRIMARY KEY CLUSTERED ([UserId] ASC, [UserRoleId] ASC),
    CONSTRAINT [FK_UserRoleMappings_Users]     FOREIGN KEY ([UserId])     REFERENCES [dbo].[Users]    ([Id]),
    CONSTRAINT [FK_UserRoleMappings_UserRoles] FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRoles] ([Id])
);
