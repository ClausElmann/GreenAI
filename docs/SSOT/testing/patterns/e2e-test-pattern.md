# e2e-test-pattern

```yaml
id: e2e_test_pattern
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/testing/patterns/e2e-test-pattern.md
red_thread: none (testing infrastructure)

project: tests/GreenAi.E2E/GreenAi.E2E.csproj
app_url: http://localhost:5057
browser: Chromium, headless=false, SlowMo=150ms
framework: Microsoft.Playwright 1.52.0 + xUnit v3

prerequisite:
  app_must_be_running: http://localhost:5057
  start_command: dotnet run --project src/GreenAi.Api/GreenAi.Api.csproj > C:\Temp\greenai-out.txt 2>&1 &
  db_must_have_seed: GreenAI_DEV — seeded by E2EDatabaseFixture

contracts:

  - name: E2ETestBase
    file: tests/GreenAi.E2E/E2ETestBase.cs
    provides:
      Page:              IPage              # Playwright page, fresh per test
      BaseUrl:           "http://localhost:5057"
      _consoleErrors:    List<string>       # captures all browser console errors + page errors
      LoginAsync():      Task               # email=admin@dev.local, password=dev123
      WaitOrFailAsync(): Task               # selector + timeout + hint → FailAsync on timeout
      FailAsync():       Task               # screenshot + URL + console + DB logs → throws
    lifecycle:
      InitializeAsync: creates browser + page + console capture hook
      DisposeAsync:    disposes browser

  - name: WaitOrFailAsync
    signature: "Task WaitOrFailAsync(string selector, int timeoutMs = 10_000, string? hint = null)"
    usage: "await WaitOrFailAsync(\"[data-testid='my-element']\", 15_000, \"hint for debug\");"
    on_timeout: calls FailAsync — never swallows TimeoutException

  - name: FailAsync
    signature: "Task FailAsync(string reason)"
    output_on_failure:
      - screenshot: tests/GreenAi.E2E/TestResults/Screenshots/{ClassName}_{yyyyMMdd_HHmmss}.png
      - URL: current page URL
      - browser_console: all errors captured since test start
      - db_logs: TOP 15 rows from [dbo].[Logs] ORDER BY TimeStamp DESC
    throws: Exception with full diagnostic context

  - name: LoginAsync
    signature: "Task LoginAsync(string email = \"admin@dev.local\", string password = \"dev123\")"
    flow:
      1: navigate to /login
      2: fill email + password
      3: click submit
      4: poll URL every 200ms until no longer /login (max 15s)
      5: FailAsync if still on /login after 15s

  - name: E2EDatabaseFixture
    file: tests/GreenAi.E2E/E2EDatabaseFixture.cs
    CollectionFixture: "E2ECollection"
    runs: once before entire E2E collection (not per test)
    actions:
      - DELETE excess profile mappings: ensures admin@dev.local has exactly 1 profile
      - seed validation: verify expected seed rows exist
    respawn_note: E2E tests do NOT use Respawn (unlike integration tests). DB is seeded once.

  - name: E2ECollection
    attribute: "[Collection(\"E2ECollection\")]"
    fixture:   "[CollectionDefinition(\"E2ECollection\")] class E2ECollection : ICollectionFixture<E2EDatabaseFixture>"

data_testid_contract:
  rule: ALL interactive elements + page headings MUST have data-testid attributes
  format: "{page}-{element}" e.g. "customer-admin-heading", "login-submit-button"
  why: E2E tests locate elements by data-testid — immune to text/translation changes
  enforcement: add during Blazor page authoring — not retrofittable cheaply

test_run_command:
  all: "dotnet test tests/GreenAi.E2E/GreenAi.E2E.csproj -v q"
  single: "dotnet test tests/GreenAi.E2E/GreenAi.E2E.csproj --filter \"TestName\" -v n"

rules:
  MUST:
    - Extend E2ETestBase (or implement IAsyncLifetime with same browser setup)
    - Use WaitOrFailAsync — never raw Page.WaitForSelectorAsync with bare catch
    - Use data-testid selectors — never text(), nth-child, or positional selectors
    - Carry [Collection("E2ECollection")] attribute
    - App MUST be running before test run
  MUST_NOT:
    - Start or stop the app as part of the test
    - Use Task.Delay for waiting — use WaitOrFailAsync
    - Share IPage across tests (one Page per test via IAsyncLifetime)
    - Read files from disk in tests

anti_patterns:

  - detect: Page.WaitForSelectorAsync with try/catch that swallows timeout
    why_wrong: failure is silent — test passes, bug is hidden
    fix: use WaitOrFailAsync — timeout → FailAsync → screenshot + logs + throw

  - detect: selector by visible text e.g. "text=Kundestyre"
    why_wrong: breaks on localization changes
    fix: "[data-testid='customer-admin-heading']"

  - detect: admin@dev.local has multiple profile mappings in test DB
    why_wrong: LoginHandler returns NeedsProfileSelection → LoginPage → empty JWT → all auth fails
    fix: E2EDatabaseFixture deletes excess mappings before collection runs

  - detect: test class NOT in E2ECollection
    why_wrong: E2EDatabaseFixture (seed/cleanup) not run — tests operate on wrong DB state
    fix: add [Collection("E2ECollection")]

  - detect: E2E test run before app is started
    why_wrong: all tests fail with connection refused immediately
    fix: start app, verify http://localhost:5057 responds, THEN run tests

enforcement:

  - where: tests/GreenAi.E2E/**/*Tests.cs
    how: must extend E2ETestBase + carry [Collection("E2ECollection")]

  - where: Blazor pages touched by E2E
    how: must have data-testid on heading and interactive elements
```
