CREATE TABLE [dbo].[UserCustomerMembership] (
    [Id]         INT                IDENTITY (1, 1) NOT NULL,
    [UserId]     INT                NOT NULL,
    [CustomerId] INT                NOT NULL,
    [Role]       NVARCHAR (50)      DEFAULT ('Member') NOT NULL,
    [IsActive]   BIT                DEFAULT ((1)) NOT NULL,
    [LanguageId] INT                DEFAULT ((1)) NOT NULL,
    [CreatedAt]  DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    CONSTRAINT [PK_UserCustomerMembership] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserCustomerMembership_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_UserCustomerMembership_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_UserCustomerMembership_UserId_CustomerId]
    ON [dbo].[UserCustomerMembership]([UserId] ASC, [CustomerId] ASC);

