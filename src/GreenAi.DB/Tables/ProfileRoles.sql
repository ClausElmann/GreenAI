CREATE TABLE [dbo].[ProfileRoles] (
    [Id]        INT             IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (100)  NOT NULL,
    [CreatedAt] DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT [PK_ProfileRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProfileRoles_Name] UNIQUE ([Name])
);
