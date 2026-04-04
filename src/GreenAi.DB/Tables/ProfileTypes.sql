CREATE TABLE [dbo].[ProfileTypes] (
    [Id]   INT            IDENTITY (1, 1) NOT NULL,
    [Key]  NVARCHAR (100) NOT NULL,
    [Name] NVARCHAR (200) NOT NULL,
    CONSTRAINT [PK_ProfileTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProfileTypes_Key] UNIQUE NONCLUSTERED ([Key] ASC)
);

