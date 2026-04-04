-- V020: ApplicationSettings (erstatter V019_SLICE000_Foundation skema)
-- Formål: Systemkonfiguration via nøgle/værdi-tabel.
--         Alle gateway-credentials, feature flags og kill-switches gemmes her.
--         Læses samlet i IApplicationSettingService (full-load cache).
--
-- Design:
--   ApplicationSettingTypeId = AppSetting enum int-value (strongly typed — ingen magic strings)
--   Name                     = enum-nøgle som string (auto-sat af Save())
--   Value                    = konfigurationsværdi (fritekst, krypteret i fase 2 for sensitiv data)
--   UpdatedAt                = UTC timestamp for seneste ændring
--
-- NOTE: V019_SLICE000_Foundation.sql oprettede ApplicationSettings med andet skema (Key/Value/ValueType).
--       Denne migration dropper den gamle tabel og opretter den korrekte version.

-- Drop gammel tabel (fra orphaned V019_SLICE000_Foundation)
IF EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.ApplicationSettings'))
    DROP TABLE [dbo].[ApplicationSettings];

CREATE TABLE [dbo].[ApplicationSettings]
(
    [Id]                        INT             IDENTITY(1,1)   NOT NULL,
    [ApplicationSettingTypeId]  INT                             NOT NULL,
    [Name]                      NVARCHAR(200)                   NOT NULL,
    [Value]                     NVARCHAR(MAX)                   NULL,
    [UpdatedAt]                 DATETIMEOFFSET                  NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT [PK_ApplicationSettings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ApplicationSettings_TypeId] UNIQUE ([ApplicationSettingTypeId])
);
