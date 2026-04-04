# handler-pattern

> **Canonical:** This is the SSOT for all MediatR handlers in GreenAi.
> **Golden samples:** `src/GreenAi.Api/Features/Auth/Login/LoginHandler.cs` (write)
>                     `src/GreenAi.Api/Features/CustomerAdmin/GetProfiles/GetProfilesHandler.cs` (read)

```yaml
id: handler_pattern
type: pattern
version: 2.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/handler-pattern.md
red_threads: [result_pattern, sql_embedded, strongly_typed_ids]
```

---

## Command (Write — input)

```csharp
// [Feature]Command.cs
namespace GreenAi.Api.Features.[Domain].[Feature];

public sealed record [Feature]Command(
    string SomeField,
    int AnotherField
) : IRequest<Result<[Feature]Response>>;
```

## Query (Read — input, no validator needed)

```csharp
// [Feature]Query.cs
namespace GreenAi.Api.Features.[Domain].[Feature];

// Parameterless query:
public sealed record [Feature]Query : IRequest<Result<[Feature]Response>>;

// Query with parameters:
public sealed record [Feature]Query(int SomeId) : IRequest<Result<[Feature]Response>>;
```

> Queries: never have a validator. Commands: always have a `[Feature]Validator.cs`.

---

## Handler (Write — with repository)

Use a repository when: handler needs >2 SQL operations OR SQL is shared across features.

```csharp
// [Feature]Handler.cs
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.[Domain].[Feature];

public sealed class [Feature]Handler : IRequestHandler<[Feature]Command, Result<[Feature]Response>>
{
    private readonly I[Feature]Repository _repository;
    private readonly ICurrentUser _currentUser;

    public [Feature]Handler(I[Feature]Repository repository, ICurrentUser currentUser)
    {
        _repository  = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<[Feature]Response>> Handle([Feature]Command command, CancellationToken ct)
    {
        var record = await _repository.FindByIdAsync(_currentUser.UserId);
        if (record is null)
            return Result<[Feature]Response>.Fail("NOT_FOUND", "Record not found.");

        await _repository.UpdateAsync(_currentUser.UserId, command.SomeField);

        return Result<[Feature]Response>.Ok(new [Feature]Response("Done"));
    }
}
```

## Handler (Read — direct IDbSession)

Use IDbSession directly when: single SQL operation, no reuse.

```csharp
// [Feature]Handler.cs (primary key: primary constructor syntax)
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.[Domain].[Feature];

public sealed class [Feature]Handler(IDbSession db, ICurrentUser user)
    : IRequestHandler<[Feature]Query, Result<[Feature]Response>>
{
    public async Task<Result<[Feature]Response>> Handle([Feature]Query _, CancellationToken ct)
    {
        var sql  = SqlLoader.Load<[Feature]Handler>("[Feature].sql");
        var rows = await db.QueryAsync<[Feature]Row>(sql, new { CustomerId = user.CustomerId.Value });
        return Result<[Feature]Response>.Ok(new [Feature]Response(rows.ToList()));
    }
}
```

## Response (Output)

```csharp
// [Feature]Response.cs
namespace GreenAi.Api.Features.[Domain].[Feature];

public sealed record [Feature]Response(string Message);
// or
public sealed record [Feature]Response(List<[Feature]Row> Items);
```

---

## Naming Convention (enforced)

```yaml
command_type:     "[Feature]Command"   # write operations (INSERT/UPDATE/DELETE)
query_type:       "[Feature]Query"     # read operations (SELECT) — never Command for pure reads
response_type:    "[Feature]Response"  # output from Command or complex Query — separate file
row_type:         "[Feature]Row"       # lightweight read projection for list/table rendering
                                       # declared inline in Handler.cs (not a separate file)

examples:
  write: LoginCommand → LoginResponse
  read_simple: GetProfilesQuery → List<ProfileRow>   (Row inline in Handler.cs)
  read_complex: MeQuery → MeResponse                 (Response has its own file)

decision_rule:
  - Result is a flat projection used by a table/list → use XxxRow (inline)
  - Result is a structured response with multiple fields → use XxxResponse (separate file)
  - Side effects involved (DB write, email, token issuance) → Command, not Query
  - Pure read, no side effects → Query, not Command

violations_fixed_2026-04-04:
  - PingCommand → PingQuery (was Command, is pure read)
```

---

## Repository (when applicable)

```csharp
// [Feature]Repository.cs
namespace GreenAi.Api.Features.[Domain].[Feature];

public interface I[Feature]Repository
{
    Task<[Feature]Record?> FindByIdAsync(UserId userId);
    Task UpdateAsync(UserId userId, string newValue);
}

public sealed class [Feature]Repository : I[Feature]Repository
{
    private readonly IDbSession _db;
    public [Feature]Repository(IDbSession db) => _db = db;

    public Task<[Feature]Record?> FindByIdAsync(UserId userId)
        => _db.QuerySingleOrDefaultAsync<[Feature]Record>(
            SqlLoader.Load<[Feature]Repository>("FindById.sql"),
            new { UserId = userId.Value });

    public Task UpdateAsync(UserId userId, string newValue)
        => _db.ExecuteAsync(
            SqlLoader.Load<[Feature]Repository>("Update.sql"),
            new { UserId = userId.Value, Value = newValue });
}
```

---

## Rules

```yaml
MUST:
  - All handlers return IRequest<Result<T>>
  - All Result failures use: Result<T>.Fail("ERROR_CODE", "message")
  - All Result successes use: Result<T>.Ok(value)
  - SQL loaded via: SqlLoader.Load<T>("File.sql")  — static call, no injection
  - ICurrentUser injected — NEVER IHttpContextAccessor
  - Strongly-typed IDs used: UserId, CustomerId, ProfileId (not raw int)
  - Handler class is sealed
  - Write commands: use repository (I[Feature]Repository interface + implementation)
  - Single-SQL reads: inject IDbSession directly
  - Auth marker: apply IRequireAuthentication to command/query interface if auth required
    → see docs/SSOT/backend/patterns/pipeline-behaviors.md

MUST_NOT:
  - Result.Success() — does not exist
  - Result.Failure() — does not exist
  - Result<T>.IsFailure — property does not exist, use !result.IsSuccess
  - Throw exceptions for expected business failures
  - Inject SqlLoader — it is always used as static SqlLoader.Load<T>()
  - Access HttpContext directly in handlers
```

---

## Error Codes

All error codes: see `docs/SSOT/backend/patterns/result-pattern.md` error_code_catalog.

**Last Updated:** 2026-04-03

```

---

## Rules

```
✅ IRequest<Result<T>>              — always Result<T>
✅ Constructor injection (primary ctor preferred)
✅ sql.Load("path/to/Feature.sql")  — never inline SQL
✅ ICurrentUser for identity (not HttpContext)
✅ IDbSession for connection
✅ Return Result.Success(value) or Result.Failure(error)
❌ No business logic in command/response records
❌ No direct EF, no repository abstraction — handlers own SQL
```

---

**Last Updated:** 2026-04-02
