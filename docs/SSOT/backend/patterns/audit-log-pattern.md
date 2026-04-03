# audit-log-pattern

```yaml
id: audit_log_pattern
type: pattern
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/audit-log-pattern.md
red_thread: audit_log → docs/SSOT/governance/RED_THREAD_REGISTRY.md

purpose: >
  Provides a persistent, tenant-scoped audit trail for sensitive user-level actions
  (e.g., email change, password reset, profile role assignment).
  The AuditLog table is the single canonical store for compliance-relevant events.

table:
  name: AuditLog
  migration: src/GreenAi.Api/Database/Migrations/V016_AuditLog.sql
  columns:
    Id:          BIGINT IDENTITY — primary key
    CustomerId:  INT NOT NULL    — tenant context (from ICurrentUser.CustomerId)
    UserId:      INT NOT NULL    — the user whose data changed (usually same as ActorId for self-service)
    ActorId:     INT NOT NULL    — the user who performed the action (important for admin impersonation)
    Action:      NVARCHAR(100)   — short event code, see action_catalog below
    Details:     NVARCHAR(2000)  — JSON or human-readable old/new values (nullable)
    OccurredAt:  DATETIMEOFFSET  — UTC at insert (DEFAULT SYSDATETIMEOFFSET())
  indexes:
    - IX_AuditLog_CustomerId_OccurredAt   # tenant-scoped time-ordered query
    - IX_AuditLog_UserId                  # user history query

action_catalog:
  # AuditLog.Action values — extend this catalog when new features write audit entries
  EMAIL_CHANGED:      "User changed their email address"
  PASSWORD_CHANGED:   "User changed their password"
  PROFILE_ASSIGNED:   "Admin assigned a profile to a user"
  PROFILE_REMOVED:    "Admin removed a profile from a user"

rules:
  MUST:
    - Write audit entries inside the same transaction as the primary operation (atomicity)
    - Populate CustomerId from ICurrentUser.CustomerId — never derive from Users row
    - Populate ActorId from ICurrentUser.UserId (even for self-service, UserId == ActorId)
    - Use a SQL file per audit operation (no inline SQL strings)
    - Action code MUST be from action_catalog above
    - New actions MUST be added to action_catalog in this file first
  MUST_NOT:
    - Write to AuditLog outside a database transaction
    - Log sensitive values (passwords, tokens) in Details
    - Use DateTime — always DATETIMEOFFSET (stored UTC)

sql_pattern:
  sql_file: >
    -- Convention: InsertAuditEntry.sql (placed in same feature folder as change operation)
    INSERT INTO AuditLog (CustomerId, UserId, ActorId, Action, Details, OccurredAt)
    VALUES (@CustomerId, @UserId, @ActorId, @Action, @Details, SYSDATETIMEOFFSET());

  repository_method: |
    Task InsertAuditEntryAsync(
        CustomerId customerId,
        UserId userId,
        UserId actorId,
        string action,
        string? details = null);

  call_site: |
    // Always call INSIDE ExecuteInTransactionAsync block
    await db.ExecuteInTransactionAsync(async () =>
    {
        await UpdateDoSomethingAsync(...);
        await InsertAuditEntryAsync(user.CustomerId!, user.UserId, user.UserId, "EMAIL_CHANGED", details: $"new={newEmail}");
    });

repository_decision_rule:
  rule: >
    Any handler that writes an audit entry will have at minimum 2 SQL operations
    (primary operation + audit insert). This always satisfies the dapper-patterns.md
    repository threshold (>=2 SQL ops). Always use IRepository pattern for features
    that require audit logging.

  reference: docs/SSOT/database/patterns/dapper-patterns.md — section "repository_vs_direct"

transaction_pattern:
  why: >
    Primary operation and audit log MUST succeed or fail atomically.
    A committed email change with no audit entry is a compliance violation.
    A rolled-back email change with an audit entry is a data integrity violation.

  how: |
    await db.ExecuteInTransactionAsync(async () =>
    {
        // 1. primary operation
        await UpdateUserEmailAsync(userId, newEmail);
        // 2. audit entry — inside same transaction
        await InsertAuditEntryAsync(customerId, userId, actorId, "EMAIL_CHANGED", $"new={newEmail}");
    });

  reference: docs/SSOT/database/patterns/dapper-patterns.md — section "transaction_pattern"

integration_with_dapper_session:
  session_method: ExecuteInTransactionAsync
  repository_interface_example: |
    public interface IChangeUserEmailRepository
    {
        Task<bool> IsEmailAvailableAsync(string email, UserId excludeUserId);
        Task UpdateEmailAsync(UserId userId, string newEmail);
        Task InsertAuditEntryAsync(CustomerId customerId, UserId userId, UserId actorId, string action, string? details);
    }

golden_sample:
  feature: ChangeUserEmail
  files:
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/ChangeUserEmailCommand.cs
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/ChangeUserEmailHandler.cs
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/ChangeUserEmailValidator.cs
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/ChangeUserEmailRepository.cs
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/ChangeUserEmailEndpoint.cs
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/CheckEmailAvailable.sql
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/UpdateUserEmail.sql
    - src/GreenAi.Api/Features/Identity/ChangeUserEmail/InsertAuditEntry.sql
```
