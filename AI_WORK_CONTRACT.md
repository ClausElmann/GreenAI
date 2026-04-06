# AI Work Contract — green-ai

> AI: Dette er din primære styringsregel. Læs den ved session-start og ved ENHVER ny opgave.

---

## STEP 0 — OBLIGATORISK FØR ALT ANDET

```
1. Match bruger-input til TRIGGER-TABEL nedenfor
2. Kør "First Tool" fra tabellen — ingen undtagelser
3. Læs det fundne dokument KOMPLET (ikke skim)
4. Implementér følgende det fundne mønster
5. Byg → 0 warnings → done
```

---

## TRIGGER-TABEL (mønster → første handling)

| Bruger siger                                            | First Tool (KØR DETTE FØRST)                                              | Derefter                            |
| ------------------------------------------------------- | ------------------------------------------------------------------------- | ----------------------------------- |
| "ny feature" / "implementér X" / "tilføj X"            | `semantic_search "X"` → find eksisterende feature                         | Læs 2-3 lig. impl. → følg mønster  |
| "ny endpoint" / "ny API" / "map post"                  | `read_file docs/SSOT/backend/patterns/endpoint-pattern.md`                | ToHttpResult(), IEndpointRouteBuilder |
| "ny handler" / "ny handler classe" / "handler mønster" | `read_file docs/SSOT/backend/patterns/handler-pattern.md`                 | Result<T>.Ok/Fail, SqlLoader.Load<T> |
| "pipeline" / "IRequireAuthentication" / "IRequireProfile" | `read_file docs/SSOT/backend/patterns/pipeline-behaviors.md`           | Marker interface decision table     |
| "dapper" / "sql loader" / "repository" / "IDbSession"  | `read_file docs/SSOT/database/patterns/dapper-patterns.md`                | SqlLoader.Load<T>, repo vs direct   |
| "transaction" / "ExecuteInTransactionAsync" / "atomisk" | `read_file docs/SSOT/database/patterns/transaction-pattern.md`            | When to use, nesting rule, test mock |
| "audit" / "audit log" / "log handling" / "trail"       | `read_file docs/SSOT/backend/patterns/audit-log-pattern.md`               | V016_AuditLog, ExecuteInTransactionAsync, action_catalog |
| "anti-pattern" / "forkert kode" / "hvad må jeg ikke"   | `read_file docs/SSOT/governance/ANTI_PATTERN_REGISTRY.md`                 | APR_001-APR_009 catalog             |
| "ny migration" / "ny tabel" / "schema"                 | `read_file docs/SSOT/database/patterns/migration-pattern.md`              | V0XX_Navn.sql, DbUp                 |
| "ny label" / "lokalisering" / "tekst"                  | `read_file docs/SSOT/localization/guides/label-creation-guide.md`         | ENESTE lovlige metode: `$labels=@(...); & scripts/localization/Add-Labels.ps1 -Labels $labels` → `Sync-Labels.ps1`. Aldrig SQL, aldrig .ps1 fil på disk, aldrig direkte mod dev DB |
| "ny test" / "skriv test" / "tilføj test"               | `read_file docs/SSOT/testing/testing-strategy.md`                         | Layer: http_integration PRIMARY     |
| "respawn" / "db reset" / "TablesToIgnore" / "fixture"  | `read_file docs/SSOT/testing/guides/respawn-guide.md`                     | Ignored tables catalog, seed rules  |
| "permission" / "IPermissionService" / "UserRole" / "ProfileRole" | `read_file docs/SSOT/identity/permissions.md`                    | Two systems: UserRoles (global) vs ProfileRoles (operational) |
| "Blazor komponent" / "reusable component" / "@Parameter"       | `read_file docs/SSOT/ui/patterns/blazor-component-pattern.md`    | Parameters in, EventCallback out, no Mediator injection       |
| "MudBlazor" / "MudTable" / "MudChip" / "MudAlert"             | `read_file docs/SSOT/ui/patterns/mudblazor-conventions.md`       | Approved components, loading/error contracts, anti-patterns   |
| "design token" / "css token" / "farve token" / "--color-"      | `read_file docs/SSOT/ui/color-system.md`                         | Token SSOT — aldrig hardcoded hex. Cascade: design-tokens.css → greenai-skin → enterprise → portal-skin |
| "css klasse" / "utility class" / ".ga-" / "inline style"       | `read_file docs/SSOT/ui/component-system.md`                     | .ga-btn-* / .ga-card / .ga-table / .ga-col-numeric / .ga-chip-reset / .ga-icon-* — governance tests håndhæver |
| "governance test" / "css compliance" / "CssTokenCompliance"    | `read_file docs/SSOT/ui/component-system.md`                     | 9 tests i CssTokenComplianceTests.cs — alle Category=Governance |
| "navigation" / "ruter" / "ui model" / "ui-navigation"         | `read_file docs/SSOT/ui/models/ui-navigation-schema.json`        | Route → auth → breadcrumb → query mapping                    |
| "test strategi" / "hvilken test" / "test layer"         | `read_file docs/SSOT/testing/testing-strategy.md`                         | Vælg lag per trigger-tabel          |
| "test automation" / "hvornår test" / "test regel"       | `read_file docs/SSOT/testing/test-automation-rules.md`                    | Trigger table → required tests      |
| "test fejler" / "debug" / "fix E2E" / "fix test"       | `read_file docs/SSOT/testing/debug-protocol.md`                           | OBSERVE→ACT, fix-layer-lock         |
| "DB fejl" / "logs" / "hvad skete der"                  | `Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" -TrustServerCertificate -Query "SELECT TOP 20 TimeStamp,Level,Message,Exception FROM Logs ORDER BY TimeStamp DESC"` | Klassificér lag fra output |
| "sql fejl" / "invalid column" / "null constraint" / "sql fejler" | `pwsh -File scripts/debug/Test-SqlStatement.ps1 -SqlFile <path> -Parameters @{...}` | 5 sek → EKSAKT fejl + fix options |
| "tool registry" / "hvilke scripts" / "hvilke tools" / "ps modul" | `read_file docs/ai-governance/tool-registry.yaml`                         | AI tool authority — alle tilladte tools |
| "auth" / "JWT" / "permission" / "ICurrentUser"         | `read_file docs/SSOT/identity/README.md`                                  | Custom JWT, ingen ASP.NET Identity  |
| "Blazor" / "razor" / "komponent" / "side"              | `semantic_search "lignende komponent"` → find eksempel                    | OnAfterRenderAsync + PrincipalHolder|
| "byg" / "build" / "compile" / "0 warnings"             | `dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q`                    | Fix alle warnings                   |
| "kør tests" / "test alle"                              | `dotnet test tests/GreenAi.Tests -v q`                                    | Skal hedde N/N passed               |
| "dokumentation" / "ny doc" / "ssot"                    | `read_file docs/SSOT/_system/ssot-document-placement-rules.md`            | <450 linjer, korrekt mappe          |
| "governance" / "red thread" / "protokol" / "mangler"   | `read_file docs/SSOT/governance/README.md`                                | Se SSOT_GAP_PLAN → sprint prioritet |
| "ssot update" / "hvornår opdater ssot" / "ssot drift"  | `read_file docs/SSOT/governance/ssot-update-protocol.md`                  | Trigger table → mandatory actions   |
| "ai grænser" / "hvad må ai" / "autonomi" / "stop"       | `read_file docs/SSOT/governance/ai-boundaries.md`                         | ALLOWED vs FORBIDDEN vs STOP_AND_ASK || \"ssot map\" / \"find ssot\" / \"trigger index\" / \"context loader\" | `read_file docs/SSOT/_system/ssot-map.json`                        | trigger → pattern → file path        |
| \"feature contract\" / \"feature map\" / \"hvilke features\" / \"EXEC_\" | `read_file docs/SSOT/_system/feature-contract-map.json`          | feature → handler → endpoint → tests || "hvad skal jeg huske" / "ny session" / "resumé"        | `read_file AI_STATE.md`                                                   | Compact: features, tests, active work, last 5 decisions. Fuld audit-log → EXECUTION_MEMORY.md |
| "fejl signal" / "kompile fejl" / "SIG_"                | `read_file docs/SSOT/governance/ERROR_DETECTION.md`                       | Klassificér → fix → log             |
| ANYTHING ELSE                                          | `grep_search "<emne>" docs/SSOT/`                                         | Læs → implementér                   |

---

## 🔴 VERIFY-FØR-ANTAG REGLEN (aldrig gæt DB-state)

**FØR du siger "tabellen mangler" / "seed data mangler" / "migration ikke kørt":**

```powershell
# Verificer altid med SQL FØRSTE — aldrig antagelser
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
  -TrustServerCertificate `
  -Query "SELECT TABLE_NAME, (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c2 WHERE c2.TABLE_NAME = t.TABLE_NAME) AS Cols FROM INFORMATION_SCHEMA.TABLES t WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"

# Check om specifikke rækker eksisterer:
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
  -TrustServerCertificate -Query "SELECT COUNT(*) AS RowCount FROM [dbo].[Labels]"
```

**ALDRIG sig:**
- ❌ "Tabellen eksisterer ikke" — RUN: `SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='X'`
- ❌ "Seed data mangler" — RUN: `SELECT COUNT(*) FROM TableName`
- ❌ "Migration ikke kørt" — RUN: `SELECT * FROM SchemaVersions`

**ALTID sig:**
- ✅ "Lad mig verificere med SQL query..."
- ✅ "VERIFICERET: 5 rækker i Labels tabel"

---

## ABSOLUTTE REGLER (binære — ingen undtagelser)

### GIT
```
❌ ALDRIG: git add / git commit / git push / git reset / git rebase
✅ Forbered commit-besked → præsenter → vent på "ja" eller "gør det"
```

### KODE
```
❌ EF Core — brug Dapper + .sql filer
❌ ASP.NET Identity — brug custom JWT (ICurrentUser)
❌ Newtonsoft.Json — brug System.Text.Json
❌ HttpContext i handlers — brug ICurrentUser
❌ SQL mod tenant-tabel uden WHERE CustomerId = @CustomerId
❌ Task.Delay i tests
❌ Hardcoded strings i Blazor — brug @Loc.Get(...)
❌ Result<T> ikke returneret fra handler

✅ Én .sql fil per DB-operation
✅ Strongly typed IDs: UserId, CustomerId, ProfileId
✅ 0 compiler warnings efter enhver ændring
✅ Result<T> fra alle handlers
✅ Valider kun ved system-grænser (ikke interne metoder)
✅ AI_STATE.md opdateres i SAMME operation som EXECUTION_MEMORY.md-entry skrives
```

### SSOT-DOCS
```
❌ Opret SSOT-fil > 600 linjer
❌ Kopiér indhold — link i stedet
❌ Justificér med "NeeoBovisWeb gør det" — justificér fra green-ai SSOT
✅ Opdatér SSOT når nyt mønster opdages
```

---

## TECH STACK (memorisér)

| Lag          | Teknologi                  |
|--------------|---------------------------|
| Runtime      | .NET 10 / C# 13           |
| Arkitektur   | Vertical Slice             |
| Frontend     | Blazor Server + MudBlazor  |
| Data         | Dapper + Z.Dapper.Plus     |
| Auth         | Custom JWT — ICurrentUser  |
| Mediator     | MediatR + FluentValidation |
| Migrationer  | DbUp (.sql filer)          |
| Tests        | xUnit v3 + NSubstitute     |
| Logging      | Serilog → SQL + console    |

---

## FEATURE-STRUKTUR (vertical slice)

```
src/GreenAi.Api/Features/[Domain]/[Feature]/
  [Feature]Command.cs      → IRequest<Result<T>>
  [Feature]Handler.cs      → AL logik her
  [Feature]Validator.cs    → AbstractValidator<TCommand>
  [Feature]Response.cs     → output record
  [Feature]Endpoint.cs     → app.MapPost(...).Map(app)
  [Feature]Page.razor      → Blazor (hvis UI)
  [Feature].sql            → ÉN sql-fil per DB-operation
```

---

## TENANT-ISOLATION

Pre-auth SQL (`FindUserByEmail`, token lookup): ingen `CustomerId` påkrævet.  
**Alt andet SQL mod tenant-ejede tabeller: `WHERE CustomerId = @CustomerId` — ALTID.**  
Autoritativ kilde: `docs/SSOT/identity/tenant-isolation.md`

---

## DEBUG-SEKVENS (rød tråd ved fejl)

```
OBSERVE: Læs fejlbesked + URL + screenshot
    ↓
KLASSIFICÉR lag: TEST | HANDLER | SQL | SCHEMA | BLAZOR | INFRA
    ↓
OBSERVE: Query Logs-tabel (se trigger-tabel ovenfor)
    ↓
ACT: FIX ÉT lag
    ↓
OBSERVE: Kør tests → verificér
    ↓
Gentag til grøn
```

Fuld protokol: `docs/SSOT/testing/debug-protocol.md`

---

**Last Updated:** 2026-04-03

