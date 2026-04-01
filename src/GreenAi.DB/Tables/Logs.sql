CREATE TABLE [dbo].[Logs] (
    [Id]              INT                IDENTITY (1, 1) NOT NULL,
    [Message]         NVARCHAR (MAX)     NULL,
    [MessageTemplate] NVARCHAR (MAX)     NULL,
    [Level]           NVARCHAR (128)     NULL,
    [TimeStamp]       DATETIMEOFFSET (7) DEFAULT (sysdatetimeoffset()) NOT NULL,
    [Exception]       NVARCHAR (MAX)     NULL,
    [Properties]      NVARCHAR (MAX)     NULL,
    [SourceContext]   NVARCHAR (256)     NULL,
    [TraceId]         NVARCHAR (64)      NULL,
    [UserId]          INT                NULL,
    [CustomerId]      INT                NULL,
    CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED ([Id] ASC)
);



GO
CREATE INDEX [IX_Logs_TimeStamp]  ON [dbo].[Logs] ([TimeStamp] DESC);
CREATE INDEX [IX_Logs_Level]      ON [dbo].[Logs] ([Level]);
CREATE INDEX [IX_Logs_CustomerId] ON [dbo].[Logs] ([CustomerId]);
CREATE INDEX [IX_Logs_TraceId]    ON [dbo].[Logs] ([TraceId]);

GO
CREATE NONCLUSTERED INDEX [IX_Logs_TraceId]
    ON [dbo].[Logs]([TraceId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Logs_TimeStamp]
    ON [dbo].[Logs]([TimeStamp] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_Logs_Level]
    ON [dbo].[Logs]([Level] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Logs_CustomerId]
    ON [dbo].[Logs]([CustomerId] ASC);

