# dapper-patterns

```yaml
id: dapper_patterns
type: pattern
ssot_source: docs/SSOT/database/patterns/dapper-patterns.md
red_threads: [sql_embedded]
applies_to: ["Features/**/Repository.cs", "**/*.sql"]
enforcement: Validate-GreenAiCompliance.ps1 SQL-001
```

> **Canonical:** This is the SSOT for all database access patterns in GreenAi.
> **Code source:** `src/GreenAi.Api/SharedKernel/Db/`

```yaml
id: dapper_patterns
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/database/patterns/dapper-patterns.md
red_threads: [sql_embedded]
```

---

## SqlLoader — Loading SQL Files

```csharp
// ✅ CORRECT — type-inferred from repository/handler class
SqlLoader.Load<MyRepository>("FindById.sql")
// → resolves to: GreenAi.Api.Features.Domain.Feature.FindById.sql

// ✅ CORRECT — explicit full resource name (rare, only for cross-feature SQL)
SqlLoader.Load("GreenAi.Api.Features.Domain.Feature.FindById.sql")

// ❌ WRONG — SqlLoader is NEVER injected
public class MyRepository(SqlLoader sql) // does not exist, it is static
```

### EmbeddedResource Setup (.csproj)

All `.sql` files under `Features/**/` are automatically embedded — **no manual ItemGroup changes needed**:

```xml
<!-- GreenAi.Api.csproj — already configured -->
<EmbeddedResource Include="Features\**\*.sql" />
<EmbeddedResource Include="Database\Migrations\*.sql" />
```

> Rule: New `.sql` files placed inside `Features/[Domain]/[Feature]/` are automatically picked up.

---

## IDbSession Methods

File: `src/GreenAi.Api/SharedKernel/Db/IDbSession.cs`

```csharp
// Returns multiple rows
Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);

// Returns single row or null (use for lookups that may not find a row)
Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null);

// Returns rows affected (INSERT, UPDATE, DELETE)
Task<int> ExecuteAsync(string sql, object? param = null);

// Wraps multiple statements in one transaction — commit on success, rollback on exception
Task ExecuteInTransactionAsync(Func<Task> work);
```

---

## Usage Examples

### Single row lookup (null if missing)

```csharp
var user = await _db.QuerySingleOrDefaultAsync<UserRecord>(
    SqlLoader.Load<MyRepository>("FindById.sql"),
    new { UserId = userId.Value });
// user is null → return Result.Fail("NOT_FOUND", ...)
```

### List query

```csharp
var rows = await _db.QueryAsync<ProfileRow>(
    SqlLoader.Load<MyHandler>("GetProfilesForCustomer.sql"),
    new { CustomerId = user.CustomerId.Value });
return Result<List<ProfileRow>>.Ok(rows.ToList());
```

### Write (no return value)

```csharp
await _db.ExecuteAsync(
    SqlLoader.Load<MyRepository>("UpdatePassword.sql"),
    new { UserId = userId.Value, PasswordHash = hash, PasswordSalt = salt });
```

### Transactional write (2+ operations)

```csharp
await _db.ExecuteInTransactionAsync(async () =>
{
    await _db.ExecuteAsync(SqlLoader.Load<MyRepo>("ResetFailedLogin.sql"), new { UserId = id.Value });
    await _db.ExecuteAsync(SqlLoader.Load<MyRepo>("InsertRefreshToken.sql"), new { /* params */ });
});
// If either throws → both rolled back automatically
```

---

## Repository vs Direct IDbSession — Decision Rule

```yaml
use_repository_when:
  - handler needs >=2 SQL operations on the same entity
  - SQL is called from multiple handlers
  - unit-testing the handler requires mocking out DB calls
  file_naming: I[Feature]Repository interface + [Feature]Repository implementation
  location: Features/[Domain]/[Feature]/[Feature]Repository.cs

use_idbs_session_directly_when:
  - single SQL operation, no reuse expected
  - handler is a simple read (query → rows → return Ok)
  file_naming: no repository file needed — inject IDbSession into handler
  location: handler constructor: (IDbSession db, ICurrentUser user)

golden_samples:
  repository: src/GreenAi.Api/Features/Auth/Login/LoginRepository.cs (multiple SQL files, reuse)
  direct_db:  src/GreenAi.Api/Features/CustomerAdmin/GetProfiles/GetProfilesHandler.cs (single SQL)
```

---

## SQL File Conventions

See: `docs/SSOT/database/patterns/sql-conventions.md`

Quick rules:
- One SQL statement per file
- File named after the operation: `FindById.sql`, `Update.sql`, `GetForCustomer.sql`
- Always use `@ParameterName` (never string concat)
- Tenant tables: always include `WHERE CustomerId = @CustomerId`

---

## Anti-patterns

```yaml
anti_patterns:

  - detect: inline SQL string in C# code
    example: 'await _db.QueryAsync<T>("SELECT * FROM Users WHERE Id = @Id", ...)'
    why_wrong: violates RED_THREAD sql_embedded; undetectable by SqlLoader resource check
    fix: move SQL to [FeatureName].sql, load via SqlLoader.Load<T>()

  - detect: SqlLoader injected as constructor parameter
    example: "public class Handler(SqlLoader sql)"
    why_wrong: SqlLoader is static — it has no instance, it cannot be injected
    fix: call StaticApiLoader.Load<T>() directly in method body

  - detect: string concatenation in SQL parameters
    example: '$"SELECT * FROM Users WHERE Email = \'{email}\'"'
    why_wrong: SQL injection vulnerability
    fix: always use parameterized: new { Email = email }

  - detect: db.QuerySingleOrDefaultAsync without null check
    example: "var user = await db...; return Result.Ok(user);"  // user could be null
    why_wrong: null dereference at caller
    fix: check null → return Result.Fail("NOT_FOUND", ...) before using value

  - detect: RowVersion column in SET clause of UPDATE statement
    example: "SET Email = @Email, RowVersion = NEWID()"
    why_wrong: >
      ROWVERSION (SQL Server TIMESTAMP) is a system-managed concurrency token.
      SQL Server increments it automatically on every row write.
      Writing to it is a compile-time SQL error: "Cannot update a timestamp column."
      This error only surfaces at runtime — the build will not catch it.
    fix: omit RowVersion from SET clause entirely
    valid_use: WHERE clause comparison for optimistic concurrency — OK to READ, forbidden to WRITE
    validated_by: Validate-GreenAiCompliance.ps1 → SQL-001 (scans .sql files)
    confirmed: APR_009 in docs/SSOT/governance/ANTI_PATTERN_REGISTRY.md
```

**Last Updated:** 2026-04-03
