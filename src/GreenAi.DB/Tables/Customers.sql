CREATE TABLE [dbo].[Customers] (
    [Id]        INT                IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (200)     NOT NULL,
    [CreatedAt] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

