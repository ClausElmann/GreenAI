# Tenant Isolation

> **Canonical:** SSOT for multi-tenant data isolation rules in green-ai.
> **Enforcement:** `Validate-GreenAiCompliance.ps1` SQL-002

```yaml
id: tenant_isolation
type: rule
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/identity/tenant-isolation.md
red_threads: [tenant_isolation, auth_flow, current_user]
related:
  - docs/SSOT/database/patterns/sql-conventions.md
  - docs/SSOT/identity/current-user.md
```

---

## Rule

**Every SQL query against a tenant-owned table MUST include `WHERE CustomerId = @CustomerId`.**

```sql
-- ✅ CORRECT — tenant-scoped
SELECT Id, Name FROM Profiles
WHERE CustomerId = @CustomerId AND IsActive = 1;

-- ❌ WRONG — missing tenant filter
SELECT Id, Name FROM Profiles WHERE IsActive = 1;
```

---

## Tenant-Owned Tables (require CustomerId filter)

| Table | Notes |
|-------|-------|
| `Customers` | The tenant root itself — filter by `Id` |
| `Profiles` | Always `CustomerId = @CustomerId` |
| `ProfileUserMappings` | Via ProfileId → CustomerId |
| `UserCustomerMemberships` | Filter by `CustomerId` |
| `UserRefreshTokens` | Filter by `CustomerId` |
| `CustomerSettings` | Directly scoped |
| `CustomerUserRoleMappings` | Directly scoped |

## Global Tables (no CustomerId filter required)

| Table | Reason |
|-------|--------|
| `Users` | Global identity — no tenant context |
| `UserRoles` / `UserRoleMappings` | Global role definitions |
| `Languages` | Reference data |
| `Countries` | Reference data |
| `Labels` | Scoped by LanguageId, not CustomerId |
| `Logs` | System table |

---

## Where to Get CustomerId

**ALWAYS** from `ICurrentUser.CustomerId.Value` — **NEVER** from request body or URL params.

```csharp
// ✅ CORRECT
await db.QueryAsync<Row>(sql, new { CustomerId = user.CustomerId.Value });

// ❌ WRONG — CustomerId from HTTP request
await db.QueryAsync<Row>(sql, new { CustomerId = command.CustomerId });
```

The JWT token carries `CustomerId` as a claim. `HttpContextCurrentUser` extracts it.

---

## Pre-Auth Exceptions

These queries run **before** a CustomerId is available:

| Query | Reason |
|-------|--------|
| `FindUserByEmail.sql` | Login — user not yet identified |
| `FindValidRefreshToken.sql` | Token refresh — customer re-discovered from token |

These queries operate on `Users` (global) or match by token — no tenant filter needed.

---

## Enforcement

`Validate-GreenAiCompliance.ps1` rule **SQL-002** checks:
- SQL files referencing `Profiles`, `ProfileUserMappings`, `CustomerSettings` must contain `CustomerId`

Manual review required for tables not in the automated list.

---

**Last Updated:** 2026-04-06
