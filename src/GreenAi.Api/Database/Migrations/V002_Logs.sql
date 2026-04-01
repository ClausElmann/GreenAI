-- V002 Logs
-- Strukturerede logs fra Serilog (server + klient)
CREATE TABLE [dbo].[Logs]
(
    [Id]              INT IDENTITY(1,1) NOT NULL,
    [Message]         NVARCHAR(MAX)     NULL,
    [MessageTemplate] NVARCHAR(MAX)     NULL,
    [Level]           NVARCHAR(128)     NULL,
    [TimeStamp]       DATETIMEOFFSET    NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Exception]       NVARCHAR(MAX)     NULL,
    [Properties]      NVARCHAR(MAX)     NULL,   -- JSON: TraceId, UserId, CustomerId, SourceContext osv.
    [SourceContext]   NVARCHAR(256)     NULL,   -- "ClientSide", handler-navn osv.
    [TraceId]         NVARCHAR(64)      NULL,
    [UserId]          INT               NULL,
    [CustomerId]      INT               NULL,

    CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE INDEX [IX_Logs_TimeStamp]   ON [dbo].[Logs] ([TimeStamp] DESC);
CREATE INDEX [IX_Logs_Level]       ON [dbo].[Logs] ([Level]);
CREATE INDEX [IX_Logs_CustomerId]  ON [dbo].[Logs] ([CustomerId]);
CREATE INDEX [IX_Logs_TraceId]     ON [dbo].[Logs] ([TraceId]);
