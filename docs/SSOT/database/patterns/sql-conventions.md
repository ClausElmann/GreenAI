# SQL Conventions — green-ai

```yaml
id: sql_conventions
type: convention
ssot_source: docs/SSOT/database/patterns/sql-conventions.md
red_threads: [sql_embedded, tenant_isolation]
applies_to: ["**/*.sql"]
enforcement: Validate-GreenAiCompliance.ps1 SQL-001 + APR-009
```

> Naming, column types, parameterization, tenant rules.

**Last Updated:** 2026-04-02

---

## Table Naming

```
dbo.Users
dbo.Customers
dbo.UserCustomerMemberships
dbo.Labels
dbo.Countries
```

- Schema: `dbo`
- PascalCase, **always plural** — no exceptions
- Join/mapping tables: both entity names joined + `Mappings` suffix — e.g. `ProfileUserMappings`, `UserRoleMappings`, `UserCustomerMemberships`

---

## Column Conventions

| Pattern | Convention | Example |
|---------|------------|---------|
| PK | `Id INT IDENTITY(1,1)` | `Id` |
| FK | `[Entity]Id` | `CustomerId`, `LanguageId` |
| Flags | `Is[Adjective]` | `IsActive`, `IsPublished` |
| Timestamps | `CreatedAt DATETIMEOFFSET` | `CreatedAt` |
| Strings | `NVARCHAR(n)` | `NVARCHAR(200)` |
| Large text | `NVARCHAR(MAX)` | descriptions |
| Short codes | `NVARCHAR(2)` to `NVARCHAR(10)` | ISO codes |

---

## Parameterized Query Rules

```sql
-- ✅ CORRECT: named parameters
SELECT Id, Name FROM dbo.Customers
WHERE Id = @CustomerId AND IsActive = 1;

-- ❌ WRONG: string concatenation
-- NEVER: "WHERE Id = " + customerId
```

All parameters via anonymous object in Dapper:

```csharp
var result = await conn.QueryAsync<Foo>(sql, new { CustomerId = id });
```

---

## Tenant Isolation

All tenant-owned tables have a `CustomerId` column. All queries against them MUST include:

```sql
WHERE CustomerId = @CustomerId
```

**Tables NOT requiring CustomerId** (global/reference data):
- `Users` (global user directory)
- `Languages`
- `Countries`
- `Labels`
- `UserRefreshTokens` (scoped by UserId)

See [docs/SSOT/identity/tenant-isolation.md](../../identity/tenant-isolation.md).

---

## SqlLoader Pattern

SQL files are embedded resources loaded at runtime:

```csharp
// sql file path relative to src/GreenAi.Api/
var query = sql.Load("Features/Customer/GetCustomer/GetCustomer.sql");
```

File must be marked `Build Action = Embedded Resource` in `.csproj` (handled by glob in project file).

---

**Last Updated:** 2026-04-02
