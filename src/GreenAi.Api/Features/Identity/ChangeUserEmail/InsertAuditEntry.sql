-- InsertAuditEntry.sql
INSERT INTO AuditLog (CustomerId, UserId, ActorId, Action, Details)
VALUES (@CustomerId, @UserId, @ActorId, @Action, @Details);
