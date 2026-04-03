# PATTERN_EXTRACTION

```yaml
id: pattern_extraction
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/PATTERN_EXTRACTION.md

purpose: Rules for detecting when logic must be extracted into reusable SSOT.

trigger_rules:

  - id: repeat_2x
    detect: same code structure written in 2+ places
    action: create pattern SSOT before 3rd occurrence
    where: Features/**/*.cs, Components/**/*.razor
    examples:
      - OnAfterRenderAsync + PrincipalHolder.Set + Mediator.Send → blazor-page-pattern.md
      - Result<T>.Fail("NO_CUSTOMER", ...) guard → result-pattern.md
      - sql.Load(...) + db.Connection.QuerySingleAsync → dapper-patterns.md

  - id: implicit_assumption
    detect: code that works only due to undocumented convention
    action: document convention in relevant SSOT area
    examples:
      - BlazorPrincipalHolder must be Scoped — never Singleton/Transient
      - E2EDatabaseFixture must delete excess profile mappings per [known_issue]
      - ALL LoginStatus variants must be handled explicitly in LoginPage

  - id: error_code_invented
    detect: Result.Fail() with code NOT in ResultExtensions.cs
    action:
      1: add code to ResultExtensions.cs with correct HTTP status
      2: update docs/SSOT/governance/RED_THREAD_REGISTRY.md → error_codes
      3: update docs/SSOT/backend/patterns/result-pattern.md (when created)

  - id: test_helper_repeated
    detect: same WaitForAsync / assertion pattern in 2+ test classes
    action: move to E2ETestBase or TestHelpers static class
    ssot_target: docs/SSOT/testing/patterns/e2e-test-pattern.md

extraction_steps:

  - step: 1_detect
    action: identify trigger (repeat_2x | implicit_assumption | error_code_invented | test_helper_repeated)

  - step: 2_locate_target
    action: check SSOT_GAP_PLAN.md → find pending file that covers this pattern
    fallback: check docs/SSOT/_system/ssot-document-placement-rules.md → derive correct path

  - step: 3_create_or_update
    action: create new file OR append to existing pattern file
    format: structured yaml block per SSOT template (not prose)
    size_check: result file must be < 450 lines

  - step: 4_register
    action: update area README.md with link to new file

  - step: 5_log
    action: append to EXECUTION_MEMORY.md with ssot_updated: yes

anti_patterns:

  - detect: creating identical code in Handler A and Handler B without shared pattern
    fix: extract to pattern → link both handlers to ssot_source

  - detect: writing comment explaining why something is done a certain way
    fix: remove comment → create or update SSOT entry → reference ssot_source in code comment

  - detect: test file with >3 custom helpers not in base class
    fix: move helpers to E2ETestBase or integration TestBase

do_not_extract:

  - one-off infrastructure code (migrations, DI registration) — these are not patterns
  - single-use response records — they live in [Feature]Response.cs, not SSOT
  - SQL files — these are data, not patterns
```
