# AUTO_IMPROVEMENT

```yaml
id: auto_improvement
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/AUTO_IMPROVEMENT.md

purpose: Protocol for promoting discovered improvements into SSOT and deprecating superseded patterns.

triggers:
  - improvement_found field non-empty in EXECUTION_MEMORY.md entry
  - PATTERN_EXTRACTION.md trigger fired (repeat_2x or implicit_assumption)
  - ERROR_DETECTION.md signal SIG_004 (red_thread violation) resolved with new understanding

steps:

  - step: 1_classify
    action: determine improvement type
    types:
      - new_pattern:      logic that worked but was undocumented
      - better_approach:  existing SSOT has a suboptimal pattern
      - anti_pattern:     approach that caused failure
      - missing_rule:     governance gap that allowed error

  - step: 2_locate_ssot_target
    action: find correct file in docs/SSOT/
    decision_tree:
      - pattern for handlers/endpoints → docs/SSOT/backend/patterns/
      - pattern for Blazor pages       → docs/SSOT/backend/patterns/blazor-page-pattern.md
      - auth / identity pattern        → docs/SSOT/identity/
      - SQL / Dapper pattern           → docs/SSOT/database/patterns/
      - test pattern                   → docs/SSOT/testing/patterns/
      - governance rule                → docs/SSOT/governance/
      - file does not exist            → check SSOT_GAP_PLAN.md → create per sprint priority

  - step: 3a_new_pattern
    action: create or append structured block to target file
    format: yaml block (never prose)
    required_fields: [id, type, inputs, outputs, rules.MUST, rules.MUST_NOT, anti_patterns]

  - step: 3b_better_approach
    action: update existing SSOT file
    required:
      - add new block marked: version: 2 (or increment)
      - mark old block as: status: DEPRECATED
      - add: replaced_by: [new block id]
      - update Last Updated date

  - step: 3c_anti_pattern
    action: append to anti_patterns section of relevant SSOT file
    format:
      - detect: how to recognise the pattern
      - why_wrong: what fails
      - fix: correct approach (reference new pattern or existing ssot_source)

  - step: 4_update_red_thread
    action: IF improvement affects a red_thread invariant → update RED_THREAD_REGISTRY.md
    triggers:
      - new error code added
      - new enforcement location identified
      - violation_action changed

  - step: 5_log
    action: update EXECUTION_MEMORY.md entry with ssot_updated: yes

  - step: 6_verify
    action: run dotnet build + dotnet test
    expected: 0 warnings, 0 failures

improvement_backlog:
  comment: items extracted from EXECUTION_MEMORY.md pending SSOT creation

  - id: IMP_001
    from: EXEC_003
    type: missing_rule
    description: LoginPage must handle ALL LoginStatus variants explicitly
    target_file: docs/SSOT/identity/auth-flow.md    # create in sprint_1
    current_status: pending

  - id: IMP_002
    from: EXEC_003
    type: new_pattern
    description: OnAfterRenderAsync + PrincipalHolder.Set() + Mediator.Send ordering contract
    target_file: docs/SSOT/backend/patterns/blazor-page-pattern.md    # create in sprint_1
    current_status: pending

  - id: IMP_003
    from: EXEC_003
    type: new_pattern
    description: E2EDatabaseFixture MUST delete excess profile mappings to ensure deterministic login
    target_file: docs/SSOT/testing/guides/e2e-test-pattern.md    # create in sprint_1
    current_status: pending

  - id: IMP_004
    from: EXEC_003
    type: anti_pattern
    description: BlazorPrincipalHolder registered as Singleton or Transient causes cross-circuit identity pollution
    target_file: docs/SSOT/identity/current-user.md    # create in sprint_1
    anti_pattern:
      detect: builder.Services.AddSingleton<BlazorPrincipalHolder>()
      fix: builder.Services.AddScoped<BlazorPrincipalHolder>()
    current_status: pending

deprecation_protocol:
  rule: Old patterns are never deleted — they are marked status:DEPRECATED with replaced_by reference.
  reason: Deleted patterns cannot be learned from. Deprecated patterns teach what not to do.
```
