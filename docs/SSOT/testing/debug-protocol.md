# Debug Protocol — green-ai

```yaml
id: debug_protocol
type: protocol
ssot_source: docs/SSOT/testing/debug-protocol.md
red_threads: []
applies_to: ["failing tests", "failing builds"]
enforcement: OBSERVE layer before changing code — never guess root cause
```

> Autoritativ kilde for al debugging af tests, Blazor, og DB-fejl.  
> Inspireret af NeeoBovisWeb's Integration Test Debugging Protocol v2 — tilpasset green-ai's stakke.

**Last Updated:** 2026-04-03

---

## Kerneregler (UFRAVIGELIGE)

### REGEL 1 — PING-PONG MODEL

Enhver debug-session veksler STRENGT:

```
OBSERVE → Saml hård evidens
ACT     → Anvend ÉN målrettet fix
OBSERVE → Valider fix med evidens
ACT     → Næste fix ELLER erklær succes
```

**Forbudt:**
- ❌ ACT → ACT (dobbelfix uden observation)
- ❌ Spekulativt fix (modificer kode uden evidens)
- ❌ "Det er nok X" (KRAV: "EVIDENS viser X kræver Y")

**Håndhævelse:** Før enhver kodeændring SKAL jeg angive:
```
LAST_ACTION: OBSERVE | ACT
CURRENT_ACTION: OBSERVE | ACT
VIOLATION_CHECK: PASS | FAIL
```

---

### REGEL 2 — HARD EVIDENCE GATE

Før enhver kodemodifikation SKAL alle disse eksistere:

```yaml
EVIDENCE_CHECKLIST:
  test_identity:
    - test_name: "exact method name"
    - failure_line: "line number"

  failure_signature:
    - assertion: "exact Assert.X that failed"
    - expected: "expected value"
    - actual: "actual value"

  application_state:
    - url_at_failure: "http://localhost:5057/..."
    - browser_console_errors: "any JS/Blazor errors"
    - db_logs: "relevant rows from Logs table"

  root_cause:
    - hypothesis: "singular root cause"
    - evidence_supporting: ["item 1", "item 2"]
    - fix_layer: "TEST|HANDLER|SQL|SCHEMA|BLAZOR|INFRA"
```

**Gate:** Hvis checklist ikke er komplet → HARD STOP → OBSERVE FIRST

---

### REGEL 3 — FIX-LAYER LOCK

Klassificér PRÆCIST ÉT lag per iteration:

| Layer    | Hvornår                                         |
|----------|-------------------------------------------------|
| `TEST`   | Test-logik forkert (arrange/assert)             |
| `HANDLER`| Business logic fejl i MediatR handler           |
| `SQL`    | Query syntax, parameter binding, kolonnenavn    |
| `SCHEMA` | Tabel/kolonne/constraint mismatch               |
| `BLAZOR` | Komponent-rendering, OnAfterRenderAsync, DI     |
| `INFRA`  | Startup, DI registration, konfiguration         |

Angiv altid:
```
FIX_LAYER_LOCK: [lag]
JUSTIFICATION: "1 sætning"
```

---

## Debug Workflows

### Unit Test fejler (Handler/Repository)

```
STEP 1  Læs præcis assert-fejl: expected / actual
STEP 2  Reproducér: kør kun den test
        dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~<TestName>" -v n
STEP 3  Klassificér lag: TEST vs HANDLER vs SQL vs SCHEMA
STEP 4  Verifér mod DB hvis SQL-fejl:
        → Brug DbDebugHelper.QueryLogsAsync() fra test
        → Eller Invoke-Sqlcmd direkte (se DB Debug nedenfor)
STEP 5  FIX ÉT lag
STEP 6  Kør tests → 0 warnings → done
```

### E2E test fejler (Playwright/Blazor)

```
STEP 1  Test kaster TimeoutException → læs fejlbeskeden
        → URL ved timeout angives af E2ETestBase.FailAsync()
        → Screenshot gemmes i tests/GreenAi.E2E/TestResults/Screenshots/
        → Browser console errors angives i fejlbeskeden
STEP 2  Klassificér:
        TIMEOUT_ELEMENT_MISSING  → data-testid mangler eller OnAfterRenderAsync kørte ikke
        REDIRECT_LOOP            → auth/ICurrentUser problem
        ASSERTION_MISMATCH       → forkert tekst/data vises
        BACKEND_ERROR            → tjek Logs-tabellen (se DB Debug)
STEP 3  Til BLAZOR-lag fejl: tjek OnAfterRenderAsync + PrincipalHolder.Set()
STEP 4  Til INFRA-lag fejl: tjek BlazorPrincipalHolder er Scoped + DI-kæde
STEP 5  FIX + genbyg app + kør tests igen
```

---

## DB Debug — Query Logs Under Debugging

Når du har brug for at se hvad applikationen loggede under en testsekvens:

```powershell
# Seneste 20 log-rækker fra dev-databasen
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
  -TrustServerCertificate `
  -Query "SELECT TOP 20 TimeStamp, Level, Message, Exception FROM Logs ORDER BY TimeStamp DESC"

# Kun fejl/warnings
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
  -TrustServerCertificate `
  -Query "SELECT TOP 20 TimeStamp, Level, Message, Exception FROM Logs WHERE Level IN ('Error','Warning') ORDER BY TimeStamp DESC"

# Log for specifik bruger
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
  -TrustServerCertificate `
  -Query "SELECT TOP 20 TimeStamp, Level, Message FROM Logs WHERE UserId = 1 ORDER BY TimeStamp DESC"

# Auth-state i db: profilemappings for admin
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
  -TrustServerCertificate `
  -Query "SELECT u.Email, p.DisplayName FROM ProfileUserMappings pum JOIN Users u ON u.Id = pum.UserId JOIN Profiles p ON p.Id = pum.ProfileId ORDER BY u.Email"
```

---

## E2E Test Base — Debug Helpers

`tests/GreenAi.E2E/E2ETestBase.cs` indeholder:

| Metode | Hvad den gør |
|--------|--------------|
| `FailAsync(reason)` | Screenshot + URL + console errors → kaster Exception med komplet kontekst |
| `WaitOrFailAsync(selector, timeout)` | Venter på selector — kalder `FailAsync` på timeout med diagnostik |
| `LoginAsync(email, password)` | Logger ind + poller URL væk fra /login |

Screenshot-sti ved fejl: `tests/GreenAi.E2E/TestResults/Screenshots/{TestName}_{timestamp}.png`

---

## Typiske Fejlklasser (Reference)

| Symptom | Sandsynlig årsag | Fix-lag |
|---------|-----------------|---------|
| `[data-testid='X']` timeout | `StateHasChanged()` mangler / OnAfterRenderAsync kørte ikke | BLAZOR |
| Redirect til `/login` efter nav | `[Authorize]` attribute, token ugyldig | BLAZOR/INFRA |
| Redirect til `/select-customer` | `BlazorPrincipalHolder.Set()` ikke kaldt før `Mediator.Send` | BLAZOR |
| `NO_CUSTOMER` i handler | `ICurrentUser.CustomerId` kaster — claims mangler i JWT | HANDLER |
| Test timeout fra start | App ikke startet / forkert port | INFRA |
| Assert fejler med DB-data | Respawn slettede seed data | TEST |

---

## Rød Tråd — Debug Sekvens

```
Fejl rapporteret
    ↓
OBSERVE: Læs præcis fejlbesked + URL + screenshot
    ↓
KLASSIFICÉR lag (TEST / HANDLER / SQL / SCHEMA / BLAZOR / INFRA)
    ↓
OBSERVE: Query Logs-tabel + verificér DB-state
    ↓
ACT: FIX ÉT lag
    ↓
OBSERVE: Kør tests → verificér fix
    ↓
Gentag OBSERVE→ACT til grøn
```

**Vigtigst:** ALDRIG kode-ændring uden evidens. ALDRIG to ændringer uden observation imellem.
