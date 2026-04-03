# testing-strategy

```yaml
id: testing_strategy
type: protocol
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/testing/testing-strategy.md

principle: >
  HTTP integration tests are the PRIMARY source of truth.
  They test the full vertical slice end-to-end (SQL → handler → endpoint → HTTP response).
  Unit tests cover pure logic only. E2E tests cover critical UI flows only.

testing_layers:

  - type: http_integration
    priority: CRITICAL
    purpose: >
      Verify that endpoints return correct HTTP status codes and response shapes.
      Tests the full slice: validation → pipeline → handler → SQL → response.
      These are the canonical correctness proof for every feature.
    scope:
      - all Minimal API endpoints
      - all Result<T> error code → HTTP status mappings
      - happy path + invalid input + unauthorized + edge cases
    infrastructure:
      fixture: DatabaseFixture (Respawn reset per test, DbUp migrations on first run)
      db: GreenAI_DEV (LocalDB — no separate test DB)
      http_client: WebApplicationFactory<Program> OR direct handler call with real IDbSession
      pattern_file: docs/SSOT/testing/patterns/http-integration-test-pattern.md
    coverage_target: 100% of endpoints

  - type: unit
    priority: LOW
    purpose: >
      Test pure business logic that is complex enough to need fast isolated verification.
      Handlers with branching logic (e.g. LoginHandler: multi-membership, lock checks).
      NOT for testing SQL or HTTP routing.
    scope:
      - handlers with non-trivial branching (>2 logical paths)
      - pure domain logic (PasswordHasher, ProfileResolutionResult)
      - pipeline behaviors
    infrastructure:
      mocking: NSubstitute
      db: none — all DB calls mocked via ILoginRepository substitute
      pattern_file: docs/SSOT/testing/patterns/unit-test-pattern.md
    coverage_target: complex logic only — NOT line coverage target

  - type: e2e
    priority: MEDIUM
    purpose: >
      Verify critical user flows work in the browser including Blazor circuit,
      JS localStorage auth, and UI rendering. These are NOT duplicates of integration tests —
      they test the Blazor rendering layer and browser interaction specifically.
    scope:
      - login → frontpage flow
      - authenticated page guard (redirect to /login if unauthenticated)
      - post-login page rendering (heading, tabs visible)
      - NOT form submission logic (covered by http_integration)
    infrastructure:
      browser: Playwright Chromium, headless=false, SlowMo=150ms
      base_class: E2ETestBase
      fixture: E2EDatabaseFixture
      pattern_file: docs/SSOT/testing/patterns/e2e-test-pattern.md
    coverage_target: critical user flows only

layer_priority_rule:
  IF endpoint has no http_integration test → BLOCKED from merge
  IF endpoint has no unit test → ACCEPTABLE if logic is simple
  IF critical UI flow has no e2e test → SHOULD be added in same sprint

primary_layer: http_integration
canonical_correctness_proof: http_integration tests

coverage_model:
  driver: endpoints (not lines of code)
  required: every endpoint must have at minimum:
    - 1 success test
    - 1 invalid_input test (if validator exists)
    - 1 unauthorized test (if RequireAuthorization applied)
  optional: edge cases (locked accounts, missing data) — add as bugs are discovered

anti_patterns:

  - detect: testing implementation details (private methods, internal state)
    why_wrong: brittle — tests break on refactor without behaviour change
    fix: test via public endpoint or handler.Handle()

  - detect: mocking everything in handler tests
    why_wrong: tests pass even when SQL is wrong
    fix: use http_integration tests for SQL correctness

  - detect: E2E test duplicates what http_integration already covers
    why_wrong: slow, fragile, and redundant
    fix: E2E covers browser/Blazor rendering only — not business logic

  - detect: shared DB state between tests (no reset)
    why_wrong: test order dependency — flaky when run in parallel
    fix: call _db.ResetAsync() in InitializeAsync, seed own data per test
```
