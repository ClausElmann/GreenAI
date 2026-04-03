# respawn-guide

> **Canonical:** SSOT for Respawn setup, TablesToIgnore rules, and seed data restoration in GreenAi tests.
> **Code sources:**
> - `tests/GreenAi.Tests/DatabaseFixture.cs` (unit/integration fixture)
> - `tests/GreenAi.E2E/E2EDatabaseFixture.cs` (E2E fixture)

```yaml
id: respawn_guide
type: guide
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/testing/guides/respawn-guide.md
red_threads: []
related:
  - docs/SSOT/testing/testing-strategy.md
  - docs/SSOT/testing/patterns/e2e-test-pattern.md
```

---

## What Respawn Does

Respawn deletes all rows from all tables in the schema **except `TablesToIgnore`**.
It does NOT drop or alter tables — schema is unchanged.

```
Before each test:  ResetAsync() → DELETE rows in dependency order
After each test:   tables are empty (except ignored)
```

This gives each test a clean slate without re-running migrations.

---

## Two Fixtures — Purpose and Scope

| Fixture | Location | Used By | Reset Trigger |
|---|---|---|---|
| `DatabaseFixture` | `tests/GreenAi.Tests/DatabaseFixture.cs` | Unit/integration tests | `InitializeAsync()` per test class |
| `E2EDatabaseFixture` | `tests/GreenAi.E2E/E2EDatabaseFixture.cs` | E2E/Playwright tests | Once per collection |

---

## DatabaseFixture Setup

```csharp
_respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
{
    DbAdapter = DbAdapter.SqlServer,
    SchemasToInclude = ["dbo"],
    TablesToIgnore = [
        new Respawn.Graph.Table("dbo", "SchemaVersions"),   // DbUp metadata — never delete
        new Respawn.Graph.Table("dbo", "UserRoles"),        // reference data — seeded at migration time
        new Respawn.Graph.Table("dbo", "ProfileRoles"),     // reference data — seeded at migration time
        new Respawn.Graph.Table("dbo", "Languages"),        // reference data
        new Respawn.Graph.Table("dbo", "Countries"),        // reference data
    ]
});
```

### TablesToIgnore Decision Rule

```yaml
ignore_when:
  - table is seeded by a migration and never changes per-test (reference data)
  - table is DbUp metadata (SchemaVersions)
  - deleting the table would break FK constraints on reference data

do_NOT_ignore_when:
  - table holds test data (Users, Customers, Profiles, AuditLog, etc.)
  - test needs a clean slate for isolation
  - table is an audit/log table where stale rows would corrupt assertions

rule: >
  When adding a new migration that seeds reference data:
  ADD that table to TablesToIgnore in DatabaseFixture.cs + E2EDatabaseFixture implicitly
  (E2EDatabaseFixture uses raw SQL, not Respawn — it handles its own seed restoration)
```

### How to Reset Between Tests

```csharp
// In your test class (integration tests only):
public sealed class MyRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;

    public MyRepositoryTests(DatabaseFixture db) => _db = db;

    public ValueTask InitializeAsync() => _db.ResetAsync();  // ← Respawn delete here
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

---

## E2EDatabaseFixture — Seed Restoration (No Respawn)

E2E tests use a different approach: instead of Respawn, `E2EDatabaseFixture.InitializeAsync()`
uses idempotent `IF NOT EXISTS / INSERT` SQL to ensure the required seed data exists.

```
E2E seed data (in GreenAI_DEV):
  - Customer:  Testkommune
  - Users:     admin@dev.local, sender@dev.local (password: "Test1234")
  - Profiles:  Nordjylland, Sønderjylland
  - Mappings:  admin → Nordjylland (one profile → auto-select at login)
               sender → Nordjylland
```

### Critical Known Issue: Profile Mapping Cleanup

```yaml
known_issue: >
  If admin@dev.local has >1 ProfileUserMappings row (e.g. from a previous test run
  that created an extra mapping), LoginHandler requires profile selection — E2E login
  flow fails because the test doesn't expect a profile selection step.

fix: >
  E2EDatabaseFixture explicitly DELETES extra mappings before inserting the expected ones:
    DELETE FROM ProfileUserMappings WHERE ProfileId = {profile2Id} AND UserId = {adminId}
  Then inserts the canonical single mapping.

why_this_matters: >
  After full E2E test runs, extra assertions or test data may leave the DB in a state
  where admin has 2 profile mappings. The DELETE before INSERT in E2EDatabaseFixture
  is the determinism guard — never remove it.
```

---

## Adding a New Table to TablesToIgnore

When a new reference-data table is created (seeded in migration, never per-test):

1. Add migration entry (V0XX_Create.sql) with seed INSERT
2. Open `tests/GreenAi.Tests/DatabaseFixture.cs`
3. Add to `TablesToIgnore`:
   ```csharp
   new Respawn.Graph.Table("dbo", "YourNewTable"),
   ```
4. Run `dotnet test tests/GreenAi.Tests -v q` → confirm all tests pass
5. Update this file (`respawn-guide.md`) with the new table in the "Current Ignored Tables" catalog below

---

## Current Ignored Tables Catalog

| Table | Reason |
|---|---|
| `SchemaVersions` | DbUp migration metadata — must never be deleted |
| `UserRoles` | Reference data seeded in migration — not per-test data |
| `ProfileRoles` | Reference data seeded in migration — not per-test data |
| `Languages` | Reference data seeded in migration (at least Language Id=1) |
| `Countries` | Reference data seeded in migration |

---

## Anti-patterns

```yaml
- detect: test class does NOT call _db.ResetAsync() in InitializeAsync
  why_wrong: stale rows from previous test leak into current test — assertions can pass for wrong reasons
  fix: always implement IAsyncLifetime; call _db.ResetAsync() in InitializeAsync

- detect: assertion checks COUNT(*) but rows from other tests exist
  why_wrong: test is not isolated — count includes rows it didn't create
  fix: call ResetAsync first; or scope query by UserId/CustomerId from test's own seed data

- detect: new reference table NOT added to TablesToIgnore
  why_wrong: Respawn deletes reference data on every test reset — queries that require
             reference rows (e.g. Language FK) fail with FK violation
  fix: add to TablesToIgnore in DatabaseFixture + document in this catalog

- detect: E2EDatabaseFixture seed inserts without DELETE guard on profile mappings
  why_wrong: extra mappings cause LoginHandler to require profile selection → E2E login fails
  fix: always DELETE extra mappings before expected INSERT (see Critical Known Issue above)
```
