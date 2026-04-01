-- V003 Add PasswordSalt, FailedLoginCount, IsLockedOut, LastFailedLoginAt to Users
ALTER TABLE [dbo].[Users]
    ADD [PasswordSalt]       NVARCHAR(512)   NOT NULL DEFAULT '',
        [FailedLoginCount]   INT             NOT NULL DEFAULT 0,
        [IsLockedOut]        BIT             NOT NULL DEFAULT 0,
        [LastFailedLoginAt]  DATETIMEOFFSET  NULL;
