CREATE TABLE [dbo].[Profiles] (
    [Id]            INT                IDENTITY (1, 1) NOT NULL,
    [CustomerId]    INT                NOT NULL,
    [DisplayName]   NVARCHAR (200)     NOT NULL,
    [CreatedAt]     DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [ProfileTypeId] INT                NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_Profiles_ProfileTypes] FOREIGN KEY ([ProfileTypeId]) REFERENCES [dbo].[ProfileTypes] ([Id])
);

