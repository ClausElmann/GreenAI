-- V011: Countries, Labels (LocaleStringResources)
--
-- SOURCE SYSTEM ANALYSIS (analysis-tool localization domain):
--   Countries   — reference table for country/language mapping (Id 1–6 = DK/SE/EN/FI/NO/DE)
--                 CountryId and LanguageId share the same 1–6 numeric space.
--                 Not queried at runtime in source system — used as referential/seed data.
--   Labels      — LocaleStringResources from source system.
--                 Key: ResourceName (dot-separated, e.g. 'shared.DateFormatShort')
--                 Uniqueness: (LanguageId, ResourceName) — composite unique index.
--                 Missing translations stored as sentinel value 'Missing Translation'.
--
-- All tables are idempotent (IF NOT EXISTS guards).

-- -----------------------------------------------------------------------
-- Countries
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.Countries'))
BEGIN
    CREATE TABLE [dbo].[Countries] (
        [Id]               INT             IDENTITY (1, 1) NOT NULL,
        [Name]             NVARCHAR (100)  NOT NULL,
        [TwoLetterIsoCode] NVARCHAR (2)    NOT NULL,    -- 'DK', 'SE', 'GB', 'FI', 'NO', 'DE'
        [ThreeLetterIsoCode] NVARCHAR (3)  NOT NULL,    -- 'DNK', 'SWE', 'GBR', 'FIN', 'NOR', 'DEU'
        [NumericIsoCode]   SMALLINT        NOT NULL,
        [PhoneCode]        INT             NOT NULL,    -- +45, +46, +44, +358, +47, +49
        [LanguageId]       INT             NOT NULL,    -- FK → Languages.Id (1:1 mapping)
        [DisplayOrder]     INT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Countries_Languages] FOREIGN KEY ([LanguageId]) REFERENCES [dbo].[Languages] ([Id])
    );

    CREATE UNIQUE INDEX [UIX_Countries_TwoLetterIsoCode] ON [dbo].[Countries] ([TwoLetterIsoCode]);
END;

-- Seed: 6 Nordic + DE countries (Id values match LanguageId 1–6)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Countries] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[Countries] ON;

    INSERT INTO [dbo].[Countries] ([Id], [Name], [TwoLetterIsoCode], [ThreeLetterIsoCode], [NumericIsoCode], [PhoneCode], [LanguageId], [DisplayOrder])
    VALUES
        (1, 'Denmark',     'DK', 'DNK', 208,  45, 1, 1),
        (2, 'Sweden',      'SE', 'SWE', 752,  46, 2, 2),
        (3, 'United Kingdom', 'GB', 'GBR', 826, 44, 3, 3),
        (4, 'Finland',     'FI', 'FIN', 246, 358, 4, 4),
        (5, 'Norway',      'NO', 'NOR', 578,  47, 5, 5),
        (6, 'Germany',     'DE', 'DEU', 276,  49, 6, 6);

    SET IDENTITY_INSERT [dbo].[Countries] OFF;
END;

-- -----------------------------------------------------------------------
-- Labels (LocaleStringResources)
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.Labels'))
BEGIN
    CREATE TABLE [dbo].[Labels] (
        [Id]            INT             IDENTITY (1, 1) NOT NULL,
        [LanguageId]    INT             NOT NULL,
        [ResourceName]  NVARCHAR (200)  NOT NULL,   -- dot-separated key, e.g. 'shared.DateFormatShort'
        [ResourceValue] NVARCHAR (MAX)  NOT NULL,   -- translated string
        [CreatedAt]     DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [UpdatedAt]     DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [PK_Labels] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Labels_Languages] FOREIGN KEY ([LanguageId]) REFERENCES [dbo].[Languages] ([Id])
    );

    -- Primary read path: look up a single label by (LanguageId, ResourceName)
    CREATE UNIQUE INDEX [UIX_Labels_LanguageId_ResourceName]
        ON [dbo].[Labels] ([LanguageId] ASC, [ResourceName] ASC)
        INCLUDE ([ResourceValue]);

    -- Secondary read path: find all languages for a given ResourceName (admin UI)
    CREATE INDEX [IX_Labels_ResourceName_LanguageId]
        ON [dbo].[Labels] ([ResourceName] ASC, [LanguageId] ASC)
        INCLUDE ([ResourceValue]);
END;
