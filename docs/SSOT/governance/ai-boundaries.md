# ai-boundaries

```yaml
id: ai_boundaries
type: rule
ssot_source: docs/SSOT/governance/ai-boundaries.md
red_threads: []
applies_to: ["AI execution loop"]
enforcement: STOP_AND_ASK before git operations, schema drops, production mutations
```

> **Canonical:** Defines what AI agents are permitted to do autonomously vs. what requires human confirmation.
> **Red thread:** governance — derived from AI_WORK_CONTRACT.md ABSOLUTTE REGLER

```yaml
id: ai_boundaries
type: rule
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/ai-boundaries.md

purpose: >
  Prevents AI from taking irreversible or high-blast-radius actions autonomously.
  Separates safe local exploration from destructive/shared-state mutations.

rule: When in doubt → STOP_AND_ASK. It costs 10 seconds. A wrong git push costs hours.
```

---

## ALLOWED — AI may do these autonomously

```yaml
allowed:
  source_code:
    - create new files (any extension)
    - edit existing source files (.cs, .razor, .sql, .md, .json, .ps1, .py)
    - delete generated files (bin/, obj/ contents)
    - rename files that have not been committed
    - add using/import statements
    - register DI services in Program.cs
    - add endpoint Map() registrations in Program.cs

  build_and_test:
    - dotnet build
    - dotnet test (all patterns)
    - run governance scripts (Validate-GreenAiCompliance.ps1, Validate-Audit-Package.ps1)
    - run Create-Audit-Package.ps1
    - run DbUp migrations on dev (GreenAI_DEV LocalDB)

  ssot:
    - create new SSOT files (docs/SSOT/**)
    - update existing SSOT files
    - update AI_WORK_CONTRACT.md
    - update EXECUTION_MEMORY.md
    - update SSOT_GAP_PLAN.md
    - update RED_THREAD_REGISTRY.md
    - update ANTI_PATTERN_REGISTRY.md
    - update MASTER_BUILD_PLAN.md

  database_dev_only:
    - read queries on GreenAI_DEV (SELECT, EXPLAIN)
    - run DbUp migrations (idempotent — safe)
    - INSERT/UPDATE in integration/E2E test execution context (tests own their data + Respawn resets)
```

---

## FORBIDDEN — STOP immediately, present plan, wait for user "yes"

```yaml
forbidden:

  git:
    - git add
    - git commit
    - git push
    - git reset --hard / git reset (any destructive variant)
    - git rebase / git rebase --interactive
    - git merge (without user review)
    why: >
      These affect shared history, remote state, or discard uncommitted work.
      AI prepares commit message and file list, then waits for user to confirm.
    exception: none — no exceptions

  file_deletion:
    - delete source files (.cs, .razor, .sql, .md) that have been committed
    - delete migration files (V0XX_*.sql) — ever
    - delete docs/ or SSOT files
    why: >
      Deletion of committed files requires git history context that AI may not have.
      Migration files are permanent schema history — never deleted.
    action: present list of files to delete, explain why, wait for "yes"

  database_mutations:
    - DROP TABLE / DROP COLUMN / TRUNCATE / DELETE without WHERE on prod or shared DB
    - ALTER TABLE on production
    - UPDATE without WHERE on prod
    - Any mutation on a non-dev, non-test database
    why: data loss in shared / prod environments is irreversible
    action: present SQL, explain impact, require explicit confirmation

  architecture:
    - change SharedKernel interfaces (ICurrentUser, IDbSession, Result<T>) without SSOT update
    - add pipeline behavior to Program.cs middleware without updating pipeline-behaviors.md
    - change JWT token structure without updating auth-flow.md
    why: these changes affect every handler/endpoint — blast radius is entire codebase

  external_systems:
    - POST/PUT/DELETE to any external API
    - send SMS / email
    - deploy to any environment
    why: external effects are irreversible and may affect real users
```

---

## STOP_AND_ASK Conditions

```yaml
stop_and_ask_when:
  - task is ambiguous and 2+ interpretations exist
  - AI would need to delete >3 source files to complete the task
  - task requires a production DB query
  - task requires pushing to remote git
  - task requires modifying shared infrastructure (CI/CD, DNS, certs)
  - conflicting requirements detected between SSOT and user request
  - architecture invariant would be violated to satisfy request

stop_format: |
  STOP_AND_ASK: [one sentence describing what needs clarification]
  Option A: [safe interpretation]
  Option B: [alternative interpretation]
  Waiting for confirmation.
```

---

## Autonomous Loop Rules

```yaml
autonomous_loop:
  # When running the global loop (EXECUTE_SLICE repeated):
  max_files_per_slice: 10
  max_slices_before_summary: 5    # after 5 slices, output full iteration summary to user
  stop_on_first_build_warning: true
  stop_on_first_test_failure: true
  stop_on_governance_violation: true
  never_skip_validation: true     # build + test + compliance required after every slice

  good_stopping_points:
    - after every 3 slices (report progress to user)
    - when a STOP_00X condition fires
    - when next slice would require file deletion
    - when sprint_4 items are only remaining (all LOW priority)
```

---

## Self-Evaluation Rule

```yaml
self_evaluation:
  after_each_slice: |
    1. Did I introduce any warnings? → if yes: STOP_003
    2. Did I touch a file outside this slice's scope? → if yes: revert extra changes
    3. Did I skip SSOT update for a new pattern? → if yes: create SSOT before continuing
    4. Did I create a new Result<T>.Fail() code without registering it? → STOP_005
    5. Did something fail that I expected to succeed? → add to ANTI_PATTERN_REGISTRY

  question_to_ask_each_iteration: >
    "If another AI agent reads ONLY the SSOT files, can it reproduce exactly what I just built?"
    If NO → SSOT is incomplete → update before continuing.
```
