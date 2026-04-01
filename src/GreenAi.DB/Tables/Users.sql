CREATE TABLE [dbo].[Users] (
    [Id]                INT                IDENTITY (1, 1) NOT NULL,
    [Email]             NVARCHAR (256)     NOT NULL,
    [PasswordHash]      NVARCHAR (512)     NOT NULL,
    [IsActive]          BIT                DEFAULT ((1)) NOT NULL,
    [CreatedAt]         DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [RowVersion]        ROWVERSION         NOT NULL,
    [PasswordSalt]      NVARCHAR (512)     DEFAULT ('') NOT NULL,
    [FailedLoginCount]  INT                DEFAULT ((0)) NOT NULL,
    [IsLockedOut]       BIT                DEFAULT ((0)) NOT NULL,
    [LastFailedLoginAt] DATETIMEOFFSET (7) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);





GO
CREATE UNIQUE INDEX [UIX_Users_Email] ON [dbo].[Users] ([Email]);
