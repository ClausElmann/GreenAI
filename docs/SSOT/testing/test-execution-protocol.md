# test-execution-protocol

```yaml
id: test_execution_protocol
type: protocol
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/testing/test-execution-protocol.md

commands:
  unit_and_integration:
    run: dotnet test tests/GreenAi.Tests -v q
    requires: GreenAI_DEV LocalDB reachable
    duration: <60 seconds

  e2e:
    run: dotnet test tests/GreenAi.E2E -v q
    requires: GreenAi.Api running on port 5057
    start_api: dotnet run --project src/GreenAi.Api/GreenAi.Api.csproj --urls "http://localhost:5057"
    duration: 60-120 seconds

  specific_test:
    run: dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~[TestName]" -v n
    use_when: debugging a single failing test

  build_check:
    run: dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q
    gate: 0 errors, 0 warnings — MUST pass before running tests
```

## Execution Order

```
1. Build          dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q
                  → STOP if any errors or warnings

2. Unit + HTTP    dotnet test tests/GreenAi.Tests -v q
                  → STOP if any test fails, fix before moving to E2E

3. E2E            dotnet test tests/GreenAi.E2E -v q
                  → Requires app started on port 5057
                  → STOP if fails — check debug-protocol.md for diagnosis steps
```

## DB Reset Mechanism

```yaml
db_reset:
  tool: Respawn (DbUp-aware)
  scope: data only (not schema)
  tables_preserved:
    - SchemaVersions       # migration tracking
    - UserRoles            # reference data
    - ProfileRoles         # reference data
    - Languages            # reference data
    - Countries            # reference data
  reset_trigger: _db.ResetAsync() called in each test's InitializeAsync()
  isolation_model: per test — each test seeds its OWN data → no shared state
```

## Failure Diagnosis Protocol

When a test fails, diagnose by layer:

```yaml
diagnosis_steps:

  step_1_identify_layer:
    unit_test_fails:
      likely_cause: mock setup wrong, handler logic changed
      check: read error, verify Substitute.For<I> setup matches handler's calls
    repository_test_fails:
      likely_cause: SQL wrong, schema mismatch, missing seed data
      check: run query manually in SSMS against GreenAI_DEV

  step_2_sql_debugging:
    run_query_directly: |
      Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" `
                    -Database "GreenAI_DEV" `
                    -TrustServerCertificate `
                    -Query "SELECT TOP 10 * FROM [TableName]"
    check_logs: |
      Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" `
                    -Database "GreenAI_DEV" `
                    -TrustServerCertificate `
                    -Query "SELECT TOP 20 TimeStamp,Level,Message,Exception FROM Logs ORDER BY TimeStamp DESC"

  step_3_e2e_debugging:
    likely_causes:
      - timeout: element not found within WaitOrFailAsync timeout
      - wrong_selector: data-testid changed, check .razor file
      - not_authenticated: JWT not propagated to localStorage
    fix:
      - increase timeout in WaitOrFailAsync call
      - verify data-testid attribute exists in Blazor component
      - check E2ETestBase login flow executes correctly

  step_4_isolation_check:
    if_flaky: ensure _db.ResetAsync() is called — shared state is the most common cause
    if_order_dependent: test has dependency on prior test — each must seed independently
```

## Pre-commit Gate

Before committing any code change:

```
✅ dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q  → 0 warnings
✅ dotnet test tests/GreenAi.Tests -v q                   → all pass
✅ [if UI change] dotnet test tests/GreenAi.E2E -v q      → all pass
```

## Coverage Model

```yaml
coverage_model:
  driver: endpoints (one test class per endpoint)
  NOT: line coverage percentage
  NOT: branch coverage percentage

  required:
    - every Minimal API endpoint has [Feature]RepositoryTests
    - every branch in complex handler has [Feature]HandlerTests
    - login flow, page guard, first-render covered in E2E

  target_numbers:
    integration_endpoints: 100%  (every endpoint has at least 1 test)
    unit_handler_branches: 80%   (complex handlers covered)
    e2e_critical_flows:    3     (login, page guard, customer-admin load)
```
