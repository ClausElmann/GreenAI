-- V012: Add ISO lookup columns to Languages and Countries
--
-- Languages gets Iso639_1 (e.g. 'da', 'sv') — explicit ISO 639-1 code.
-- UniqueSeoCode was serving this purpose implicitly; Iso639_1 makes it formal.
--
-- Countries gets no new columns — TwoLetterIsoCode (ISO 3166-1 alpha-2),
-- ThreeLetterIsoCode (ISO 3166-1 alpha-3) and NumericIsoCode are already present.
-- A unique index on NumericIsoCode is added for direct ISO numeric lookups.
--
-- All changes are idempotent.

-- -----------------------------------------------------------------------
-- Languages — add Iso639_1 (ISO 639-1 two-letter language code)
-- -----------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Languages') AND name = 'Iso639_1'
)
BEGIN
    ALTER TABLE [dbo].[Languages]
        ADD [Iso639_1] NVARCHAR (2) NULL;

    -- Populate from existing UniqueSeoCode (same value — making the ISO meaning explicit)
    -- Dynamic SQL required: SQL Server compiles the batch before ALTER TABLE runs,
    -- so referencing the new column directly in the same batch causes a parse error.
    EXEC sp_executesql N'UPDATE [dbo].[Languages] SET [Iso639_1] = [UniqueSeoCode]';

    -- Now make it NOT NULL and add unique index
    ALTER TABLE [dbo].[Languages]
        ALTER COLUMN [Iso639_1] NVARCHAR (2) NOT NULL;

    CREATE UNIQUE INDEX [UIX_Languages_Iso639_1] ON [dbo].[Languages] ([Iso639_1]);
END;

-- -----------------------------------------------------------------------
-- Countries — add unique index on NumericIsoCode for ISO numeric lookups
-- -----------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Countries') AND name = 'UIX_Countries_NumericIsoCode'
)
BEGIN
    CREATE UNIQUE INDEX [UIX_Countries_NumericIsoCode]
        ON [dbo].[Countries] ([NumericIsoCode]);
END;
