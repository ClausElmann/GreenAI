# SQL Pattern — Embedded SQL via SqlLoader

> **Canonical:** SSOT for loading embedded `.sql` resources in green-ai.
> **Code source:** `src/GreenAi.Api/SharedKernel/Db/SqlLoader.cs`

```yaml
id: sql_pattern
type: pattern
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/backend/patterns/sql-pattern.md
red_threads: [sql_embedded]
related:
  - docs/SSOT/database/patterns/dapper-patterns.md
  - docs/SSOT/database/patterns/sql-conventions.md
```

---

## Rule

```
✅ ONE .sql file per database operation
✅ Load via SqlLoader.Load<T>("FileName.sql")
✅ SQL files must be embedded resources in the .csproj
❌ NO inline SQL strings in C# ("SELECT * FROM ...")
❌ NO string interpolation into SQL (use @Param always)
```

---

## Pattern

### 1. Create the .sql file

```
Features/Auth/Login/FindUserByEmail.sql
```

```sql
SELECT u.Id, u.Email, u.PasswordHash, u.PasswordSalt, u.FailedLoginCount, u.IsLockedOut
FROM Users u
WHERE u.Email = @Email;
```

### 2. Register as embedded resource in .csproj

```xml
<ItemGroup>
  <EmbeddedResource Include="Features\**\*.sql" />
</ItemGroup>
```

### 3. Load in handler or repository

```csharp
// T = the handler or repository class in the same namespace as the .sql file
var sql = SqlLoader.Load<LoginRepository>("FindUserByEmail.sql");
var user = await db.QuerySingleOrDefaultAsync<LoginUserRow>(sql, new { Email = command.Email });
```

---

## SqlLoader.Load<T> — Namespace Convention

`Load<T>` constructs the resource name as `{typeof(T).Namespace}.{fileName}`.

```
Type:      GreenAi.Api.Features.Auth.Login.LoginRepository
FileName:  FindUserByEmail.sql
Resource:  GreenAi.Api.Features.Auth.Login.FindUserByEmail.sql
```

> The `.sql` file must be in the **same folder** as the type `T`. Never reference cross-folder.

---

## Anti-patterns

```yaml
anti_patterns:
  - detect: inline SQL string concatenation
    example: 'var sql = "SELECT * FROM Users WHERE Id = " + userId;'
    fix: Use SqlLoader + parameterized @UserId

  - detect: SqlLoader.Load<WrongHandler>("SQL in different folder")
    example: 'SqlLoader.Load<GetUsersHandler>("GetProfileById.sql")'
    fix: Use SqlLoader.Load<GetProfilesHandler>("GetProfileById.sql")

  - detect: SQL file not EmbeddedResource
    symptom: FileNotFoundException at runtime
    fix: Add <EmbeddedResource Include="Features\**\*.sql" /> to .csproj
```

---

**Last Updated:** 2026-04-06
