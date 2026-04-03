# ENFORCEMENT_PROTOCOL

```yaml
id: enforcement_protocol
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md
authority: docs/SSOT/governance/EXECUTION_PROTOCOL.md

purpose: Defines HARD STOP conditions and the gate structure for all AI actions.

enforcement:

  pre_execution:
    gates:
      - id: gate_ssot_exists
        check: relevant SSOT file for the pattern has been read
        fail: STOP → read area README → find canonical file → read_file it
        signals: [SIG_003 from ERROR_DETECTION.md]

      - id: gate_red_thread_bound
        check: at least 1 red_thread from RED_THREAD_REGISTRY.md identified
        fail: STOP → read RED_THREAD_REGISTRY.md → identify applicable threads
        signals: [SIG_004 from ERROR_DETECTION.md]

      - id: gate_gap_cleared
        check: if task requires a PENDING ssot file → that file must be created first
        fail: STOP → check SSOT_GAP_PLAN.md → create missing file → proceed
        priority_order: [sprint_1 items block all new features]

  runtime:
    checks:
      - id: check_pattern_compliance
        description: code produced follows SSOT pattern exactly
        points:
          - handler returns IRequest<Result<T>>
          - endpoint calls .ToHttpResult()
          - SQL loaded via SqlLoader (not inline string)
          - ICurrentUser injected (not IHttpContextAccessor)
          - StronglyTypedIds used (not raw int)
          - Error.Code from ResultExtensions.cs
          - Blazor page uses OnAfterRenderAsync (not OnInitializedAsync)
          - PrincipalHolder.Set before first Mediator.Send

      - id: check_tenant_guard
        description: all SQL on tenant tables has WHERE CustomerId = @CustomerId
        points:
          - pre_auth exceptions: FindUserByEmail, FindValidRefreshToken (no CustomerId required)
          - all others: HARD STOP if CustomerId missing

  post_execution:
    gates:
      - id: gate_build_clean
        check: dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q
        expected: "0 Error(s), 0 Warning(s)"
        fail: FIX ALL — never proceed with warnings or errors

      - id: gate_tests_pass
        check: dotnet test tests/GreenAi.Tests -v q
        expected: "N passed, 0 failed"
        fail: debug per debug-protocol.md → fix root cause (not symptom)

      - id: gate_ssot_updated
        check: if new pattern emerged → update ssot_source file
        trigger: new error code | new behavior | new anti_pattern | new convention
        fail: update SSOT before next task

stop_conditions:

  - id: STOP_001
    condition: SSOT file for required pattern does not exist
    action:
      1: check SSOT_GAP_PLAN.md for sprint priority
      2: create file per SSOT template
      3: resume task

  - id: STOP_002
    condition: requirement is ambiguous (multiple valid interpretations)
    action:
      1: state the 2 interpretations explicitly
      2: ask user to choose
      3: document answer in relevant SSOT file
      4: resume

  - id: STOP_003
    condition: code change would violate a red_thread invariant
    action:
      1: name the violated red_thread
      2: show what would be required to allow the exception
      3: wait for explicit SSOT update authorising the exception

  - id: STOP_004
    condition: same logic found in 2+ places without SSOT reference (pattern drift)
    action:
      1: identify trigger per PATTERN_EXTRACTION.md
      2: create or update pattern SSOT file
      3: add ssot_source reference to both occurrences

  - id: STOP_005
    condition: git push / commit / reset / merge requested
    action:
      1: prepare commit message
      2: present to user
      3: WAIT for "ja" or "gør det" before executing

  - id: STOP_006
    condition: prod DB mutation (INSERT/UPDATE/DELETE on production)
    action: ASK USER with explicit statement, SQL shown, wait for confirmation per statement

  - id: STOP_007
    condition: source file deletion requested
    action: confirm with user before any rm / File.Delete / Remove-Item

forbidden:
  - push_to_git           → STOP_005
  - delete_source_files   → STOP_007
  - bypass_ssot           → STOP_001
  - inline_sql            → check_pattern_compliance violation → fix
  - ef_core               → HARD STOP, no exception
  - aspnet_identity       → HARD STOP, no exception
  - newtonsoft_json       → HARD STOP, no exception
  - task_delay_in_tests   → HARD STOP, replace with WaitOrFailAsync
  - prod_db_mutations     → STOP_006
  - hardcoded_ui_strings  → HARD STOP, replace with @Loc.Get(...)
```
