# Database — SSOT

> Authoritative patterns for all database work: migrations, SQL, schema conventions.

**Last Updated:** 2026-04-02

---

## Quick Navigation

### Patterns

| File | Topic |
|------|-------|
| [migration-pattern.md](patterns/migration-pattern.md) | DbUp V0XX naming, file placement, rollback strategy |
| [sql-conventions.md](patterns/sql-conventions.md) | Naming, column types, tenant isolation rules |
| [dapper-pattern.md](patterns/dapper-pattern.md) | Dapper + SqlLoader, parameterized queries |

### Architecture

| File | Topic |
|------|-------|
| [schema-overview.md](architecture/schema-overview.md) | All tables, FKs, tenant ownership map  |

---

## Core Rules

```
✅ DbUp only — no EF migrations
✅ Naming: V001_Description.sql, V002_Description.sql ...
✅ Files in: src/GreenAi.Api/Database/Migrations/
✅ All tenant-owned tables have CustomerId column
✅ All SQL against tenant tables: WHERE CustomerId = @CustomerId
✅ Seed data via migration (not code)
❌ No EF Core, no LINQ-to-SQL
❌ No implicit tenant filtering
❌ No global query filters
```

---

## Current Schema (V001–V012)

| Migration | Content |
|-----------|---------|
| V001 | Logs table (Serilog sink) |
| V002–V005 | Users, Customers, Profiles, Memberships |
| V006 | UserRefreshTokens |
| V007 | UserRoles + UserRoleMappings |
| V008 | RefreshToken: ProfileId + LanguageId columns |
| V009 | ProfileUserMappings (many-to-many) |
| V010 | Languages table + seed (da/sv/en/fi/nb/de) |
| V011 | Countries + Labels tables + seed |
| V012 | Iso639_1 on Languages, UIX on Countries.NumericIsoCode |

See [architecture/schema-overview.md](architecture/schema-overview.md) for full column details.

---

## Tenant Isolation Rule

**Pre-auth queries** (FindUserByEmail, token lookup) — `CustomerId` NOT required.  
**All other queries against tenant-owned tables** — MUST include `WHERE CustomerId = @CustomerId`.

See [docs/SSOT/identity/tenant-isolation.md](../identity/tenant-isolation.md) for complete rule.

---

**Last Updated:** 2026-04-02
