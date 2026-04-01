CREATE TABLE [dbo].[ProfileUserMappings] (
    [ProfileId]  INT  NOT NULL,
    [UserId]     INT  NOT NULL,
    CONSTRAINT [PK_ProfileUserMappings] PRIMARY KEY CLUSTERED ([ProfileId] ASC, [UserId] ASC),
    CONSTRAINT [FK_ProfileUserMappings_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id]),
    CONSTRAINT [FK_ProfileUserMappings_Users]    FOREIGN KEY ([UserId])    REFERENCES [dbo].[Users]    ([Id])
);
