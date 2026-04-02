CREATE TABLE [dbo].[Labels] (
    [Id]            INT                IDENTITY (1, 1) NOT NULL,
    [LanguageId]    INT                NOT NULL,
    [ResourceName]  NVARCHAR (200)     NOT NULL,
    [ResourceValue] NVARCHAR (MAX)     NOT NULL,
    [CreatedAt]     DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [UpdatedAt]     DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    CONSTRAINT [PK_Labels] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Labels_Languages] FOREIGN KEY ([LanguageId]) REFERENCES [dbo].[Languages] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Labels_ResourceName_LanguageId]
    ON [dbo].[Labels]([ResourceName] ASC, [LanguageId] ASC)
    INCLUDE([ResourceValue]);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Labels_LanguageId_ResourceName]
    ON [dbo].[Labels]([LanguageId] ASC, [ResourceName] ASC)
    INCLUDE([ResourceValue]);

