# transaction-pattern

> **Canonical:** SSOT for atomic multi-step database operations in GreenAi.
> **Code source:** `src/GreenAi.Api/SharedKernel/Db/DbSession.cs`
> **Interface:** `src/GreenAi.Api/SharedKernel/Db/IDbSession.cs`

```yaml
id: transaction_pattern
type: pattern
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/database/patterns/transaction-pattern.md
red_threads: [sql_embedded]
related: docs/SSOT/database/patterns/dapper-patterns.md
```

---

## The Method

```csharp
Task ExecuteInTransactionAsync(Func<Task> work);
```

### How It Works (DbSession internals)

```
1. Opens connection (lazy — if not already open)
2. Calls Connection.BeginTransaction() — stores as _activeTransaction
3. All subsequent ExecuteAsync() calls participate in the transaction automatically
   (DbSession.ExecuteAsync passes transaction: _activeTransaction)
4. Awaits work()
5. On success: Commit()
6. On any exception: Rollback() → re-throws
7. Finally: Dispose transaction, clear _activeTransaction
```

> **Critical:** Only `ExecuteAsync()` participates in the transaction.
> `QueryAsync` and `QuerySingleOrDefaultAsync` do NOT receive the transaction parameter
> (reads outside transaction — this is intentional: reads are non-destructive).

---

## When to Use

```yaml
use_when:
  - 2+ write operations must succeed or fail together
  - audit log must commit atomically with the primary operation
  - token rotation: revoke old + insert new must be atomic
  - any scenario where partial success is a data integrity violation

do_not_use_when:
  - single write operation (no benefit, extra overhead)
  - read-only handlers (QueryAsync never needs a transaction)
  - reads before a single write (reads can happen before the transaction opens)
```

---

## Usage Pattern

```csharp
// ✅ CORRECT — multiple writes wrapped in one transaction
await _db.ExecuteInTransactionAsync(async () =>
{
    await _db.ExecuteAsync(
        SqlLoader.Load<MyRepo>("PrimaryOperation.sql"),
        new { /* params */ });

    await _db.ExecuteAsync(
        SqlLoader.Load<MyRepo>("InsertAuditEntry.sql"),
        new { /* params */ });
});
// Both commit or both rollback — atomic
```

```csharp
// ✅ CORRECT — token rotation (real example: RefreshTokenHandler.cs)
await _db.ExecuteInTransactionAsync(async () =>
{
    await _repository.RevokeTokenAsync(record.Id);
    await _tokenWriter.SaveAsync(userId, customerId, profileId, newToken, expiry, languageId);
});
```

```csharp
// ❌ WRONG — writes outside transaction (no atomicity guarantee)
await _db.ExecuteAsync(SqlLoader.Load<MyRepo>("Write1.sql"), params1);
await _db.ExecuteAsync(SqlLoader.Load<MyRepo>("Write2.sql"), params2);
// If Write2 fails: Write1 is already committed — data is inconsistent
```

---

## Nesting Rule

```yaml
nesting:
  supported: false
  reason: >
    DbSession stores a single _activeTransaction field.
    Calling ExecuteInTransactionAsync while already inside a transaction
    would overwrite _activeTransaction, corrupting the outer transaction state.
  rule: NEVER nest ExecuteInTransactionAsync calls
  alternative: >
    If a helper method also calls ExecuteAsync, it will automatically participate
    in the already-open transaction — no inner transaction call needed.
```

---

## Repository Integration

```csharp
// When a repository method contains 2+ ExecuteAsync calls:
// ✅ Wrap them in ExecuteInTransactionAsync inside the repository

public Task UpdateEmailAndAuditAsync(UserId userId, CustomerId customerId, string newEmail)
    => _db.ExecuteInTransactionAsync(async () =>
    {
        await _db.ExecuteAsync(
            SqlLoader.Load<ChangeUserEmailRepository>("UpdateUserEmail.sql"),
            new { UserId = userId.Value, NewEmail = newEmail });

        await _db.ExecuteAsync(
            SqlLoader.Load<ChangeUserEmailRepository>("InsertAuditEntry.sql"),
            new { /* audit params */ });
    });
```

---

## Unit Test Pattern

When unit-testing a handler that calls a repository which uses `ExecuteInTransactionAsync`,
substitute the repository (not IDbSession) — the transaction is an implementation detail
of the repository, invisible to the handler.

```csharp
// ✅ In handler tests: substitute the repository interface
var repo = Substitute.For<IChangeUserEmailRepository>();
repo.UpdateEmailAndAuditAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>())
    .Returns(Task.CompletedTask);
// Transaction is hidden inside the real repository — handler tests never see IDbSession
```

When integration-testing the repository itself, use `DatabaseFixture` — the real
`DbSession` is created with `DatabaseFixture.ConnectionString`:

```csharp
private static ChangeUserEmailRepository CreateRepository() =>
    new(new DbSession(DatabaseFixture.ConnectionString));
```

---

## Real Usages (Golden Samples)

| Handler | Transaction Purpose |
|---|---|
| `RefreshTokenHandler.cs` | Revoke old token + save new token atomically |
| `LoginHandler.cs` | Reset failed login count + save refresh token atomically |
| `ChangeUserEmailRepository.cs` | Update email + write audit entry atomically |

---

## Anti-patterns

```yaml
- detect: 2+ ExecuteAsync calls outside transaction
  why_wrong: partial failure leaves data inconsistent
  fix: wrap both in ExecuteInTransactionAsync

- detect: QueryAsync inside ExecuteInTransactionAsync
  why_wrong: reads don't participate in transactions — moving them inside adds no benefit
  fix: run reads before the ExecuteInTransactionAsync block

- detect: nested ExecuteInTransactionAsync
  why_wrong: DbSession._activeTransaction is a single field — nesting overwrites it
  fix: inner method calls ExecuteAsync directly; outer caller owns the transaction
```
