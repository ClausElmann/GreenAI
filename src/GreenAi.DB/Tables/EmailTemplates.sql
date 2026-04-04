CREATE TABLE [dbo].[EmailTemplates] (
    [Id]         INT                IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (100)     NOT NULL,
    [LanguageId] INT                NOT NULL,
    [Subject]    NVARCHAR (500)     NOT NULL,
    [BodyHtml]   NVARCHAR (MAX)     NOT NULL,
    [UpdatedAt]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([LanguageId]) REFERENCES [dbo].[Languages] ([Id])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_EmailTemplates_Name_LanguageId]
    ON [dbo].[EmailTemplates]([Name] ASC, [LanguageId] ASC);

