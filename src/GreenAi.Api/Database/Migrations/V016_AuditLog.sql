-- V016_AuditLog
-- Depends: V001 (Customers, Users)
-- Purpose: Audit trail for sensitive user-level actions (email change, password reset, role change, etc.)
--
-- Design decisions:
--   CustomerId: tenant context of the acting user (from ICurrentUser.CustomerId)
--   UserId    : the user whose data was changed (not the actor — typically the same user for self-service)
--   ActorId   : the user who performed the action (important for admin impersonation scenarios)
--   Action    : short code, e.g. EMAIL_CHANGED, PASSWORD_CHANGED, PROFILE_ASSIGNED
--   Details   : JSON or human-readable detail string (old/new values where appropriate)
--   OccurredAt: UTC timestamp at insert time

CREATE TABLE AuditLog
(
    Id          BIGINT          IDENTITY(1,1) PRIMARY KEY,
    CustomerId  INT             NOT NULL REFERENCES Customers(Id),
    UserId      INT             NOT NULL REFERENCES Users(Id),
    ActorId     INT             NOT NULL REFERENCES Users(Id),
    Action      NVARCHAR(100)   NOT NULL,
    Details     NVARCHAR(2000)  NULL,
    OccurredAt  DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET()
);

CREATE INDEX IX_AuditLog_CustomerId_OccurredAt ON AuditLog (CustomerId, OccurredAt DESC);
CREATE INDEX IX_AuditLog_UserId ON AuditLog (UserId);
