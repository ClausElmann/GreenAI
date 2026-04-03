# ssot-update-protocol

> **Canonical:** This is the SSOT for when and how SSOT files must be updated.
> **Red thread:** All SSOT drift prevention — derived from RED_THREAD_REGISTRY.md

```yaml
id: ssot_update_protocol
type: rule
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/ssot-update-protocol.md

purpose: >
  Defines mandatory SSOT update triggers. Without this rule, SSOT drifts silently
  from the codebase within one sprint of new pattern introduction.

rule: SSOT update is NOT optional — it is part of the same atomic operation as the code change.
```

---

## Trigger Table — When MUST SSOT Be Updated

| Trigger | Required SSOT Action | Enforced By |
|---|---|---|
| New error code added to `ResultExtensions.cs` | Add to `result-pattern.md` `error_code_catalog` + `ResultExtensionsTests.cs` | `RESULT-001` in Validate-GreenAiCompliance.ps1 |
| New SQL column or table added | Add/update migration in `V0XX_Naziv.sql` | DbUp migration ordering |
| New pipeline behavior added | Update `pipeline-behaviors.md` pipeline order table | Code review |
| New strongly typed ID added | Verify it appears in `current-user.md` or relevant SSOT | Code review |
| New anti-pattern confirmed | Append to `ANTI_PATTERN_REGISTRY.md` | Post-execution learning enforcer |
| New red thread discovered | Append to `RED_THREAD_REGISTRY.md` | Governed via EXECUTION_PROTOCOL.md |
| Pattern used 2+ times without SSOT doc | Create SSOT file before 3rd use | AI pre-check (STOP_001 pattern) |
| New `IRequireAuthentication` or `IRequireProfile` usage | `pipeline-behaviors.md` decision table reviewed | Code review |
| New handler emits `Result<T>.Fail("NEW_CODE")` | `ResultExtensions.cs` + `result-pattern.md` updated **before** handler is written | `RESULT-001` static scan |
| EXECUTION_MEMORY appended | `SSOT_GAP_PLAN.md` items marked COMPLETED if applicable | AI post-execution step |

---

## Forbidden (SSOT Drift Sources)

```yaml
forbidden:
  - merging code that introduces an undocumented pattern
  - using a Result<T>.Fail() code that is not in ResultExtensions.cs
  - writing SQL inline (not in a .sql file)
  - adding a DB column with no accompanying migration
  - writing a handler with manual auth check instead of IRequireAuthentication
  - creating a Blazor page with OnInitializedAsync for auth

  why_each_is_forbidden:
    inline_result_code: silently maps to HTTP 500 (RESULT-001 catches this now statically)
    inline_sql: violates sql_embedded red thread + is SQL injection risk
    manual_auth_check: duplicate logic that diverges from AuthorizationBehavior (APR_005)
    OnInitializedAsync_for_auth: fails during Blazor prerender (APR_004)
```

---

## SSOT Update Workflow

```
Code change detected OR pattern first used
  ↓
1. IDENTIFY which SSOT file owns this topic
   → use docs/SSOT/_system/ssot-document-placement-rules.md
   → use AI_WORK_CONTRACT.md trigger table
  ↓
2. READ the SSOT file completely
  ↓
3. UPDATE or CREATE with new pattern/rule/example
  ↓
4. UPDATE last_updated: date field in YAML header
  ↓
5. If new SSOT file created:
   → Update SSOT_GAP_PLAN.md (mark COMPLETED)
   → Add trigger row to AI_WORK_CONTRACT.md
  ↓
6. If new anti-pattern confirmed:
   → Append to ANTI_PATTERN_REGISTRY.md
   → Add detection rule to Validate-GreenAiCompliance.ps1
  ↓
7. BUILD → 0 warnings
8. TESTS → 0 failures
9. COMPLIANCE → 0 violations
```

---

## SSOT File Size Limits

```yaml
file_size_rules:
  ideal:     <450 lines
  hard_cap:  600 lines
  on_breach: split into sub-files, update README.md area index

enforcement:
  # No automated check yet — manual review on large SSOT files
  check_command: (Get-Content "docs/SSOT/governance/ssot-update-protocol.md").Count
```

---

## Authority Chain

```
AI_WORK_CONTRACT.md                   ← session entry point (trigger table)
  ↓
RED_THREAD_REGISTRY.md                ← non-negotiable invariants
  ↓
ssot-update-protocol.md (THIS FILE)   ← when to update / what to update
  ↓
SSOT_GAP_PLAN.md                      ← what is planned/pending
  ↓
EXECUTION_MEMORY.md                   ← what was done / what was learned
  ↓
ANTI_PATTERN_REGISTRY.md              ← confirmed failure modes
  ↓
Validate-GreenAiCompliance.ps1        ← automated enforcement
```
