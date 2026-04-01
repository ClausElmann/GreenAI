CREATE TABLE [dbo].[Languages] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (100)  NOT NULL,
    [LanguageCulture] NVARCHAR (20)   NOT NULL,
    [UniqueSeoCode]   NVARCHAR (10)   NOT NULL,
    [Published]       BIT             NOT NULL DEFAULT 1,
    [DisplayOrder]    INT             NOT NULL DEFAULT 0,
    [CreatedAt]       DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT [PK_Languages] PRIMARY KEY CLUSTERED ([Id] ASC)
);

GO
CREATE UNIQUE INDEX [UIX_Languages_LanguageCulture] ON [dbo].[Languages] ([LanguageCulture]);
