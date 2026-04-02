CREATE TABLE [dbo].[Languages] (
    [Id]              INT                IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (100)     NOT NULL,
    [LanguageCulture] NVARCHAR (20)      NOT NULL,
    [UniqueSeoCode]   NVARCHAR (10)      NOT NULL,
    [Published]       BIT                DEFAULT ((1)) NOT NULL,
    [DisplayOrder]    INT                DEFAULT ((0)) NOT NULL,
    [CreatedAt]       DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [Iso639_1]        NVARCHAR (2)       NOT NULL,
    CONSTRAINT [PK_Languages] PRIMARY KEY CLUSTERED ([Id] ASC)
);



GO
CREATE UNIQUE INDEX [UIX_Languages_LanguageCulture] ON [dbo].[Languages] ([LanguageCulture]);

GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Languages_Iso639_1]
    ON [dbo].[Languages]([Iso639_1] ASC);

