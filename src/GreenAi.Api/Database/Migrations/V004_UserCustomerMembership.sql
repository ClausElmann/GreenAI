-- V004 UserCustomerMembership
-- Introduces the membership model: a User may belong to multiple Customers.
-- LanguageId is included from the start (architect decision 2026-03-31, confidence 0.99).
-- FK LanguageId -> Languages(Id) is intentionally omitted — Languages table does not exist yet.
-- It will be added as part of the localization sprint migration.

CREATE TABLE [dbo].[UserCustomerMembership]
(
    [Id]          INT             NOT NULL IDENTITY(1,1),
    [UserId]      INT             NOT NULL,
    [CustomerId]  INT             NOT NULL,
    [Role]        NVARCHAR(50)    NOT NULL DEFAULT 'Member',
    [IsActive]    BIT             NOT NULL DEFAULT 1,
    [LanguageId]  INT             NOT NULL DEFAULT 1,
    [CreatedAt]   DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT PK_UserCustomerMembership PRIMARY KEY ([Id]),
    CONSTRAINT FK_UserCustomerMembership_Users     FOREIGN KEY ([UserId])     REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT FK_UserCustomerMembership_Customers FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id])
);

CREATE UNIQUE INDEX UIX_UserCustomerMembership_UserId_CustomerId
    ON [dbo].[UserCustomerMembership] ([UserId], [CustomerId]);
