# test-automation-rules

```yaml
id: test_automation_rules
type: rules
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/testing/test-automation-rules.md

principle: >
  Tests are not optional. Every endpoint has a required test.
  Tests are written IN THE SAME OPERATION that adds the feature.
  Not after. Not in a follow-up. In the same commit.

gate_rule:
  IF new endpoint exists with no http_integration test → STOP, write tests first
  IF new handler has complex logic AND no unit test → write tests in same operation
  IF new E2E-relevant UI flow AND no e2e test → add e2e test in same sprint
```

## Trigger Table

| Trigger | Required Tests | Optional Tests |
|---------|---------------|----------------|
| New Minimal API endpoint added | `[Feature]RepositoryTests` (http_integration) | `[Feature]HandlerTests` (unit, if complex) |
| New handler with >2 branches | `[Feature]HandlerTests` (unit) | — |
| New Blazor page with auth guard | E2E: auth redirect test | E2E: page renders correctly |
| New validator added | Test via handler/repository test (invalid input case) | Separate validator test if >5 rules |
| Bug fixed | Regression test covering exact failure scenario | — |
| Column/SQL changed | Update `[Feature]RepositoryTests` to verify new column | — |

## Test Requirements Per Layer

### HTTP Integration — Required for every endpoint

```yaml
test: [Feature]RepositoryTests
minimum_cases:
  - success: valid input → repository returns expected data
  - not_found: entity does not exist → returns null OR Result.Failure
  - invalid_input: command with missing/wrong field → validator fires
  - unauthorized: endpoint requires auth, no token → 401

location: tests/GreenAi.Tests/Features/[Domain]/[Feature]RepositoryTests.cs
pattern: docs/SSOT/testing/patterns/http-integration-test-pattern.md
```

### Unit Tests — Required only when handler has complex logic

```yaml
test: [Feature]HandlerTests
required_when:
  - handler has 3+ logical branches
  - handler calls 2+ services with conditional coordination
  - business rule enforced in handler (not SQL)
not_required_when:
  - handler is pure passthrough (read query with no branching)
  - logic already covered by http_integration test

location: tests/GreenAi.Tests/Features/[Domain]/[Feature]HandlerTests.cs
pattern: docs/SSOT/testing/patterns/unit-test-pattern.md
```

### E2E Tests — Required only for critical flows

```yaml
test: [Flow]E2ETests
required_for:
  - login → authenticated page (core auth circuit)
  - page guard: unauthenticated → redirect to /login
  - first load of critical page (heading visible, tabs rendered)
not_required_for:
  - form submission details (http_integration covers logic)
  - error message text in UI (too fragile)

location: tests/GreenAi.E2E/Tests/[Flow]Tests.cs
pattern: docs/SSOT/testing/patterns/e2e-test-pattern.md
```

## Naming Conventions

```yaml
method_naming: "[MethodOrScenario]_[StateOrInput]_[ExpectedOutcome]"
examples:
  - GetLoginInfoAsync_ValidCredentials_ReturnsLoginInfo
  - GetLoginInfoAsync_UnknownEmail_ReturnsNull
  - GetLoginInfoAsync_LockedAccount_ReturnsNull
  - Handle_InvalidPassword_ReturnsLoginInvalidError
  - Handle_LockedAccount_ReturnsLoginLockedError

class_naming:
  repository_tests: "[Feature]RepositoryTests"
  handler_tests: "[Feature]HandlerTests"
  e2e_tests: "[Flow]Tests" or "[Feature]E2ETests"
```

## New Feature Checklist

When implementing a new vertical slice, test coverage is DONE when:

```
✅ [Feature]RepositoryTests exists with success + not_found + invalid_input
✅ [Feature]HandlerTests exists IF handler has branching logic
✅ Tests run: all pass (dotnet test tests/GreenAi.Tests -v q)
✅ 0 new compiler warnings introduced
✅ Test file added to same PR/commit as feature
```

## Debt Resolution

Known missing tests as of 2026-04-03:

```yaml
debt:
  - feature: ChangePassword
    missing:
      - ChangePasswordRepositoryTests (http_integration)
      - ChangePasswordHandlerTests (unit — has validation branching)
    priority: HIGH (feature is implemented but untested)
    action: create in next session

  - feature: RefreshToken
    status: check tests/GreenAi.Tests/Features/Auth/ — may exist
    action: verify coverage matches requirements above

  - feature: SelectCustomer
    status: check tests/GreenAi.Tests/Features/Auth/ — may exist
    action: verify coverage matches requirements above
```
