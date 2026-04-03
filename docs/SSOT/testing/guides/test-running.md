# Test Running Guide

> **Canonical:** SSOT for running tests in green-ai.

```yaml
id: test_running_guide
type: guide
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/testing/guides/test-running.md
red_threads: []
related:
  - docs/SSOT/testing/testing-strategy.md
  - docs/SSOT/testing/test-execution-protocol.md
```

---

## Quick Commands

```powershell
# Run ALL tests
dotnet test tests/GreenAi.Tests -v q

# Run a SPECIFIC test by name
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~LoginHandlerTests" -v n

# Run all tests for one domain
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~CustomerAdmin" -v q

# Run with detailed output
dotnet test tests/GreenAi.Tests -v n
```

---

## Prerequisites

Tests require a running LocalDB instance with the GreenAI_DEV database.

```powershell
# Check LocalDB is available
SqlLocalDB info MSSQLLocalDB

# Start if needed
SqlLocalDB start MSSQLLocalDB
```

The database is created and migrated automatically on first test run (DbUp in `DatabaseFixture`).

---

## Test Infrastructure

| Component | File | Purpose |
|-----------|------|---------|
| `DatabaseFixture` | `tests/GreenAi.Tests/DatabaseFixture.cs` | Shared DB setup (DbUp migration once per session) |
| `DatabaseCollection` | `tests/GreenAi.Tests/DatabaseFixture.cs` | xUnit collection — one fixture shared across all DB tests |
| `AuthTestDataBuilder` | `tests/GreenAi.Tests/Features/Auth/AuthTestDataBuilder.cs` | Seed users, customers, profiles |
| `CustomerAdminTestDataBuilder` | `tests/GreenAi.Tests/Features/CustomerAdmin/CustomerAdminTestDataBuilder.cs` | CustomerAdmin-specific seed helpers |

---

## Test Categories

| Category | Pattern | Run time |
|----------|---------|----------|
| Unit | No DB, NSubstitute mocks | < 1 second |
| DB Integration (handler) | Real LocalDB, Respawn reset | 1–5 seconds |
| DB Integration (repository) | Real LocalDB, raw SQL | 1–5 seconds |

---

## Connection String

The test connection string is in `appsettings.Test.json` or falls back to:

```
Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Tests fail with `SqlException: cannot open database` | LocalDB not started | `SqlLocalDB start MSSQLLocalDB` |
| Tests fail with `Table not found` | Migrations not run | Delete DB, re-run tests (DbUp recreates) |
| Flaky isolation failures | `ResetAsync()` not called | Add `db.ResetAsync()` to `InitializeAsync` |
| `Task.Delay` warning | xUnit1051 analyzer violation | Replace with `await TestContext.Current.CancellationToken.WaitAsync(...)` or remove |

---

**Last Updated:** 2026-04-06
