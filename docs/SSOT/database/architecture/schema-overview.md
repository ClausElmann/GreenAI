# Database Schema Overview

> **Canonical:** SSOT for the GreenAi database schema.
> **Migration source:** `src/GreenAi.Api/Database/Migrations/V001–V026`

```yaml
id: schema_overview
type: architecture
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/database/architecture/schema-overview.md
red_threads: [tenant_isolation, auth_flow, sql_embedded]
related:
  - docs/SSOT/database/patterns/dapper-patterns.md
  - docs/SSOT/database/patterns/migration-pattern.md
  - docs/SSOT/identity/tenant-isolation.md
```

---

## Identity & Auth Tables

| Table | Purpose | Tenant-scoped? |
|-------|---------|---------------|
| `Users` | Global user accounts (email + password hash) | NO — `CustomerId` removed in V007 |
| `Customers` | Tenant root — one per company | — |
| `UserCustomerMembership` | User ↔ Customer link (replaces Users.CustomerId) | YES |
| `Profiles` | Operational context within a customer | YES (`CustomerId`) |
| `ProfileUserMappings` | User ↔ Profile assignment | YES (via ProfileId → CustomerId) |
| `UserRefreshTokens` | Single-use refresh tokens (24h TTL) | YES (`CustomerId`) |

## Roles & Permissions

| Table | Purpose |
|-------|---------|
| `UserRoles` | Global role definitions (e.g., "Admin", "API") |
| `UserRoleMappings` | User ↔ UserRole. NOT tenant-scoped. |
| `ProfileRoles` | Profile-level role definitions (e.g., "Manager") |
| `ProfileRoleMappings` | Profile ↔ ProfileRole assignment |
| `CustomerUserRoleMappings` | Customer-scoped user role overrides |

## Localization

| Table | Purpose |
|-------|---------|
| `Languages` | Language catalog (ID=1 DA, ID=2 SV, ID=3 EN, ID=4 FI, ID=5 NO, ID=6 DE) |
| `Labels` | Key/value/languageId pairs — queried by `ILocalizationRepository` |

## System

| Table | Purpose |
|-------|---------|
| `Logs` | Serilog structured logs via `MSSqlServer` sink |
| `AuditLog` | Compliance audit trail (V016) — email changes etc. |
| `Countries` | ISO country lookup (V012) |

## Application Config

| Table | Key columns | Purpose |
|-------|-------------|--------|
| `ApplicationSettings` | `ApplicationSettingTypeId` INT, `Name` NVARCHAR(200), `Value` NVARCHAR(MAX), `UpdatedAt` | System config key/value. TypeId = `AppSetting` enum int. Read via `IApplicationSettingService` (cached full-load). |

## User Self-Service

| Table | Key columns | Purpose |
|-------|-------------|--------|
| `PasswordResetTokens` | `UserId` INT FK, `Token` NVARCHAR(128), `ExpiresAt` DATETIMEOFFSET, `UsedAt` DATETIMEOFFSET NULL, `CreatedAt` | Single-use tokens for password reset. 64-char hex. Expires per `AppSetting.PasswordResetTokenTtlMinutes`. |
| `EmailTemplates` | `Name` NVARCHAR(100), `LanguageId` INT FK, `Subject`, `BodyHtml`, `UpdatedAt` | Transactional email templates. Unique on (Name, LanguageId). Supports `{{token}}`, `{{name}}`, `{{link}}`, `{{ttl}}` substitution. |

---

## Tenant Isolation Rule

All rows in `Profiles`, `ProfileUserMappings`, `UserRefreshTokens`, `CustomerSettings`
must be filtered with `WHERE CustomerId = @CustomerId`.

**Pre-auth exception:** `Users` (global), `UserRoleMappings` (global).

See: [identity/tenant-isolation.md](../../identity/tenant-isolation.md)

---

## Migration Sequence

| Version | Description |
|---------|-------------|
| V001 | Baseline: Customers, Users, Profiles, UserRefreshTokens |
| V002 | Logs table (Serilog sink) |
| V003 | Users auth fields (PasswordSalt, FailedLoginCount, IsLockedOut) |
| V004 | UserCustomerMembership table |
| V005 | Backfill UserCustomerMembership from Users.CustomerId |
| V006 | Add LanguageId to UserRefreshTokens |
| V007 | Remove Users.CustomerId (now in UserCustomerMembership) |
| V008 | Add ProfileId to UserRefreshTokens |
| V009 | ProfileUserMappings table |
| V010 | Roles and Languages seed |
| V011 | Localization: Languages + Labels tables |
| V012 | ISO lookup columns (Countries) |
| V013 | Rename UserCustomerMembership → UserCustomerMemberships |
| V014 | Seed shared.* localization labels (DA + EN) |
| V015 | Dev seed data |
| V016 | AuditLog table |
| V017 | Seed shared.* + feature.* labels (DA + EN) |
| V018 | Seed auth feature labels |
| V020 | ApplicationSettings table (replaces orphaned V019 schema) |
| V021 | PasswordResetTokens table |
| V022 | EmailTemplates table + seed (password-reset DA + EN) |
| V023 | Re-seed ApplicationSettings with correct enum values |
| V024 | Re-seed EmailTemplates (fix content) |
| V025 | Seed portal/admin labels |
| V026 | Seed prod admin user |
| V027 | FK indexes: Profiles.CustomerId, UserCustomerMemberships.CustomerId, UserRefreshTokens.UserId+CustomerId, PasswordResetTokens.UserId, ProfileUserMappings.UserId, AuditLog.ActorId |

---

**Last Updated:** 2026-04-04
