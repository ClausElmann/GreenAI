CREATE TABLE [dbo].[UserRefreshTokens] (
    [Id]         INT                IDENTITY (1, 1) NOT NULL,
    [CustomerId] INT                NOT NULL,
    [UserId]     INT                NOT NULL,
    [Token]      NVARCHAR (512)     NOT NULL,
    [ExpiresAt]  DATETIMEOFFSET (7) NOT NULL,
    [UsedAt]     DATETIMEOFFSET (7) NULL,
    [CreatedAt]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [LanguageId] INT                DEFAULT ((1)) NOT NULL,
    [ProfileId]  INT                DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
);





GO
CREATE UNIQUE INDEX [UIX_UserRefreshTokens_Token] ON [dbo].[UserRefreshTokens] ([Token]);
