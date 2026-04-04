CREATE TABLE [dbo].[AuditLog] (
    [Id]         BIGINT             IDENTITY (1, 1) NOT NULL,
    [CustomerId] INT                NOT NULL,
    [UserId]     INT                NOT NULL,
    [ActorId]    INT                NOT NULL,
    [Action]     NVARCHAR (100)     NOT NULL,
    [Details]    NVARCHAR (2000)    NULL,
    [OccurredAt] DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([ActorId]) REFERENCES [dbo].[Users] ([Id]),
    FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLog_UserId]
    ON [dbo].[AuditLog]([UserId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLog_CustomerId_OccurredAt]
    ON [dbo].[AuditLog]([CustomerId] ASC, [OccurredAt] DESC);

