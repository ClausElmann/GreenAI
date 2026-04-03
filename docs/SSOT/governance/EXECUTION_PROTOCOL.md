# EXECUTION_PROTOCOL

```yaml
id: execution_protocol
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/EXECUTION_PROTOCOL.md
authority: AI_WORK_CONTRACT.md

rule: THIS PROTOCOL GOVERNS ALL AI ACTIONS IN GREEN-AI. NO EXCEPTIONS.

pre_conditions:
  - id: ssot_loaded
    check: relevant SSOT area file read_file before any code written
    fail_action: read docs/SSOT/[area]/README.md → find canonical file → read it

  - id: red_thread_bound
    check: identify which red_threads apply (minimum 1)
    fail_action: read RED_THREAD_REGISTRY.md → find matching thread → bind

  - id: gap_checked
    check: if ssot_source is PENDING → create SSOT file first
    fail_action: create missing SSOT per SSOT_GAP_PLAN.md before coding

steps:

  - step: 1_match_trigger
    action: read AI_WORK_CONTRACT.md trigger table → match user input → get first_tool
    output: first_tool identified
    time: immediate

  - step: 2_load_ssot
    action: read_file [ssot_source from matched red_thread]
    output: pattern in context
    fail_action: if file not found → check SSOT_GAP_PLAN.md sprint_1 → create file first

  - step: 3_bind_red_threads
    action: identify applicable red_threads from RED_THREAD_REGISTRY.md
    output: list of binding threads
    rule: ALL code produced must honour every bound red_thread

  - step: 4_implement
    action: produce code following SSOT pattern EXACTLY
    rules:
      - vertical_slice structure: Features/[Domain]/[Feature]/
      - IRequest<Result<T>> on all commands/queries
      - SqlLoader for all SQL — no inline strings
      - ICurrentUser — never IHttpContextAccessor in handlers
      - StronglyTypedIds — UserId, CustomerId, ProfileId
      - Error codes from ResultExtensions.cs only
    output: code files

  - step: 5_build
    action: dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q
    expected: "0 Error(s), 0 Warning(s)"
    fail_action: FIX ALL warnings and errors — never proceed with warnings

  - step: 6_test
    action: dotnet test tests/GreenAi.Tests -v q
    expected: N/N passed, 0 failed
    fail_action: fix test failures before further work

  - step: 7_update_ssot
    action: if new pattern emerged → update [ssot_source] file
    trigger: new error code | new behavior | new convention | new anti-pattern discovered
    rule: SSOT update is NOT optional when new pattern is used for the first time

post_conditions:
  - 0 compiler warnings
  - all integration tests pass
  - SSOT reflects all patterns used

forbidden:

  - id: git_operations
    description: git add / commit / push / reset / rebase / merge
    exception: NONE
    allowed_substitute: prepare commit message → present to user → wait for "ja / gør det"

  - id: delete_source_files
    description: delete, move, or rename production source files
    exception: NONE
    allowed_substitute: present change → wait for explicit confirmation

  - id: bypass_ssot
    description: write code for a pattern that has no SSOT file
    exception: NONE
    allowed_substitute: create SSOT file first (per SSOT_GAP_PLAN.md)

  - id: inline_sql
    description: SQL strings inline in C# code
    exception: NONE
    allowed_substitute: create [Feature].sql embedded resource + SqlLoader.Load(...)

  - id: ef_core
    description: any EntityFramework or LINQ-to-SQL
    exception: NONE

  - id: aspnet_identity
    description: Microsoft.AspNetCore.Identity or any IdentityUser
    exception: NONE

  - id: newtonsoft_json
    description: Newtonsoft.Json (use System.Text.Json)
    exception: NONE

  - id: task_delay_in_tests
    description: Task.Delay in any test file
    exception: NONE
    allowed_substitute: WaitOrFailAsync with selector polling

  - id: prod_db_mutations
    description: INSERT/UPDATE/DELETE against production database
    exception: requires explicit user confirmation for EACH statement

  - id: hardcoded_ui_strings
    description: string literals in .razor files
    exception: NONE
    allowed_substitute: @Loc.Get(labelKey) via ILocalizationService

stop_conditions:
  - SSOT file for required pattern does not exist AND cannot be created from known patterns
    action: STOP → ASK USER → document answer → create SSOT → proceed

  - Requirement is ambiguous
    action: STOP → ASK USER → document clarification in relevant SSOT file → proceed

  - Red thread conflict detected (change would violate a red_thread)
    action: STOP → flag violation → wait for explicit SSOT update authorizing exception
```
