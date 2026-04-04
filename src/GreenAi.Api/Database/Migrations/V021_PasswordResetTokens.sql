-- V021 PasswordResetTokens
-- Single-use tokens for password reset via email link.
-- Token is a random hex string (32 bytes / 64 chars).
-- Expires after 1 hour (controlled by handler — AppSetting.PasswordResetTokenTtlMinutes).
-- UsedAt is set when the token is consumed — reuse is rejected by FindResetToken.sql.
-- Idempotent: IF NOT EXISTS guard.

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.PasswordResetTokens')
)
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [Id]        INT              IDENTITY(1,1) PRIMARY KEY,
        [UserId]    INT              NOT NULL REFERENCES [dbo].[Users]([Id]),
        [Token]     NVARCHAR(128)    NOT NULL,
        [ExpiresAt] DATETIMEOFFSET   NOT NULL,
        [UsedAt]    DATETIMEOFFSET   NULL,
        [CreatedAt] DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE UNIQUE INDEX [UIX_PasswordResetTokens_Token]
        ON [dbo].[PasswordResetTokens] ([Token]);
END;
