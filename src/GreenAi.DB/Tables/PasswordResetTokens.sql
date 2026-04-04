CREATE TABLE [dbo].[PasswordResetTokens] (
    [Id]        INT                IDENTITY (1, 1) NOT NULL,
    [UserId]    INT                NOT NULL,
    [Token]     NVARCHAR (128)     NOT NULL,
    [ExpiresAt] DATETIMEOFFSET (7) NOT NULL,
    [UsedAt]    DATETIMEOFFSET (7) NULL,
    [CreatedAt] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_PasswordResetTokens_Token]
    ON [dbo].[PasswordResetTokens]([Token] ASC);

