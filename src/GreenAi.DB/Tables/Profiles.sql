CREATE TABLE [dbo].[Profiles] (
    [Id]          INT                IDENTITY (1, 1) NOT NULL,
    [CustomerId]  INT                NOT NULL,
    [DisplayName] NVARCHAR (200)     NOT NULL,
    [CreatedAt]   DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
);

