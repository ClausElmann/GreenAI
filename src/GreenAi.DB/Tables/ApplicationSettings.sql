CREATE TABLE [dbo].[ApplicationSettings] (
    [Id]                       INT                IDENTITY (1, 1) NOT NULL,
    [ApplicationSettingTypeId] INT                NOT NULL,
    [Name]                     NVARCHAR (200)     NOT NULL,
    [Value]                    NVARCHAR (MAX)     NULL,
    [UpdatedAt]                DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    CONSTRAINT [PK_ApplicationSettings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ApplicationSettings_TypeId] UNIQUE NONCLUSTERED ([ApplicationSettingTypeId] ASC)
);

