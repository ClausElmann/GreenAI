# ERROR_DETECTION

```yaml
id: error_detection
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/ERROR_DETECTION.md

purpose: Signals that require immediate action. Each signal maps to a classified error type and fix protocol.

signal_types:
  - BUILD_VIOLATION
  - TEST_FAILURE
  - SSOT_BREACH
  - RED_THREAD_VIOLATION
  - PATTERN_DRIFT

signals:

  - id: SIG_001
    type: BUILD_VIOLATION
    detect: "dotnet build" output contains "Warning(s): N" where N > 0
    meaning: compiler warning = technical debt introduced
    action:
      1: fix warning in same operation (never defer)
      2: re-run build
      3: proceed only when "0 Warning(s)"
    red_thread: zero_warnings

  - id: SIG_002
    type: TEST_FAILURE
    detect: "dotnet test" output contains "Failed: N" where N > 0
    meaning: regression or new code breaks existing contract
    action:
      1: read docs/SSOT/testing/debug-protocol.md → OBSERVE phase
      2: classify failure layer (data | auth | blazor | endpoint | test_setup)
      3: fix root cause (not symptom)
      4: re-run tests → confirm N=0
    red_thread: result_pattern

  - id: SIG_003
    type: SSOT_BREACH
    detect: code written without reading ssot_source first
    meaning: pattern may deviate from canonical approach
    signals_of_breach:
      - handler does not return Result<T>
      - endpoint does not call .ToHttpResult()
      - Blazor page uses OnInitializedAsync for auth logic
      - SQL written inline (not via SqlLoader)
      - IHttpContextAccessor injected into handler (not ICurrentUser)
    action:
      1: STOP current work
      2: read relevant SSOT file
      3: refactor to match pattern
      4: re-run build + tests

  - id: SIG_004
    type: RED_THREAD_VIOLATION
    detect: code violates an invariant in RED_THREAD_REGISTRY.md
    violation_table:
      - violates: result_pattern     → handler missing Result<T> return
      - violates: auth_flow          → ProfileId(0) issued in JWT
      - violates: current_user       → HttpContext used directly in handler
      - violates: tenant_isolation   → SQL without WHERE CustomerId = @CustomerId
      - violates: vertical_slice     → feature files outside Features/[Domain]/[Feature]/
      - violates: sql_embedded       → inline SQL string in C# code
      - violates: strongly_typed_ids → raw int used for UserId/CustomerId/ProfileId
      - violates: error_codes        → error code not in ResultExtensions.cs
      - violates: zero_warnings      → build produces warnings
      - violates: no_hardcoded_strings → string literal in .razor file
    action:
      1: STOP
      2: fix violation
      3: verify fix builds cleanly
      4: if new pattern discovered → update RED_THREAD_REGISTRY.md

  - id: SIG_005
    type: PATTERN_DRIFT
    detect: same logic appears in 2+ places without shared SSOT reference
    meaning: knowledge is implicit and will diverge
    action:
      1: detect via grep_search for duplicated block
      2: trigger PATTERN_EXTRACTION.md → extract_steps
    patterns_to_monitor:
      - OnAfterRenderAsync + PrincipalHolder.Set pattern (grep: PrincipalHolder.Set)
      - Result.Fail("NO_CUSTOMER") guard (grep: NO_CUSTOMER)
      - sql.Load + db.Connection.Query pattern (grep: sql.Load)
      - WaitOrFailAsync usage (grep: WaitOrFailAsync)

  - id: SIG_006
    type: TEST_FAILURE
    detect: E2E test times out waiting for selector
    meaning: one of 4 possible layers failed
    classification:
      - url_is_login_on_timeout:     auth failure → check JWT / LoginPage status handling
      - url_is_select_customer:      BlazorPrincipalHolder not set → check OnAfterRenderAsync order
      - url_is_target_page:          Blazor render issue → check _loading flag + StateHasChanged
      - browser_console_has_errors:  JS/Blazor circuit error → check component lifecycle
    diagnostic_command: >
      Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV"
      -TrustServerCertificate
      -Query "SELECT TOP 15 TimeStamp,Level,Message,Exception FROM Logs ORDER BY TimeStamp DESC"
    protocol: docs/SSOT/testing/debug-protocol.md

audit_rules:

  - id: AUDIT_001
    type: audit
    detect: EXECUTION_MEMORY.md not updated after task completion
    meaning: learning loop is broken
    action: append entry before ending session

  - id: AUDIT_002
    type: audit
    detect: SSOT_GAP_PLAN.md item in sprint_1 is pending for >1 feature that needed it
    meaning: same undocumented pattern used twice — extraction overdue
    action: create SSOT file immediately per PATTERN_EXTRACTION.md

  - id: AUDIT_003
    type: audit
    detect: docs/SSOT/governance/ has file with prose paragraphs (not structured yaml)
    meaning: file violates AI-optimized format rule
    action: refactor to yaml block format
```
