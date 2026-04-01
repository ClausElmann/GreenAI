CREATE TABLE [dbo].[ProfileRoleMappings] (
    [ProfileId]     INT  NOT NULL,
    [ProfileRoleId] INT  NOT NULL,
    CONSTRAINT [PK_ProfileRoleMappings] PRIMARY KEY CLUSTERED ([ProfileId] ASC, [ProfileRoleId] ASC),
    CONSTRAINT [FK_ProfileRoleMappings_Profiles]     FOREIGN KEY ([ProfileId])     REFERENCES [dbo].[Profiles]    ([Id]),
    CONSTRAINT [FK_ProfileRoleMappings_ProfileRoles] FOREIGN KEY ([ProfileRoleId]) REFERENCES [dbo].[ProfileRoles] ([Id])
);
