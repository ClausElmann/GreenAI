CREATE TABLE [dbo].[UserRoles] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (100)     NOT NULL,
    [CreatedAt]   DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [Description] NVARCHAR (500)     NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_UserRoles_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);

