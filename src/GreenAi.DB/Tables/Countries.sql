CREATE TABLE [dbo].[Countries] (
    [Id]                 INT            IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (100) NOT NULL,
    [TwoLetterIsoCode]   NVARCHAR (2)   NOT NULL,
    [ThreeLetterIsoCode] NVARCHAR (3)   NOT NULL,
    [NumericIsoCode]     SMALLINT       NOT NULL,
    [PhoneCode]          INT            NOT NULL,
    [LanguageId]         INT            NOT NULL,
    [DisplayOrder]       INT            DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Countries_Languages] FOREIGN KEY ([LanguageId]) REFERENCES [dbo].[Languages] ([Id])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Countries_NumericIsoCode]
    ON [dbo].[Countries]([NumericIsoCode] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Countries_TwoLetterIsoCode]
    ON [dbo].[Countries]([TwoLetterIsoCode] ASC);

