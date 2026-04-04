# AI Agent Instructions — green-ai

## SESSION START — KØR DETTE ALTID FØRST

```
read_file AI_WORK_CONTRACT.md
→ match bruger-input til trigger-tabel
→ kør "First Tool" fra tabellen
→ læs fundne doc KOMPLET
→ implementér
```

---

## TECH STACK

| Lag          | Teknologi                        |
|--------------|----------------------------------|
| Runtime      | .NET 10 / C# 13                  |
| Arkitektur   | Vertical Slice (feature-mappe)   |
| Frontend     | Blazor Server + MudBlazor 8      |
| Data         | Dapper + Z.Dapper.Plus (NO EF)   |
| Auth         | Custom JWT — ICurrentUser        |
| Mediator     | MediatR + FluentValidation       |
| Migrationer  | DbUp — V001_Navn.sql             |
| Tests        | xUnit v3 + NSubstitute           |
| Logging      | Serilog → [dbo].[Logs] + console |

---

## FORBUDT (❌ = compile-fejl eller governance-brud)

```
❌ EF Core / LINQ-to-SQL
❌ ASP.NET Identity
❌ Newtonsoft.Json
❌ HttpContext i handlers → brug ICurrentUser
❌ SQL uden WHERE CustomerId = @CustomerId (tenant-tabeller)
❌ Task.Delay i tests
❌ Hardcoded strings i Blazor → @Loc.Get(...)
❌ ProfileId(0) — aldrig udsted token med nul-profil
❌ SSOT-fil > 600 linjer
❌ git commit/push/reset uden bruger-godkendelse
❌ SQL INSERT direkte mod [dbo].[Labels] (dev ELLER live) — labels KUN via API
❌ .ps1 filer på disk til label-oprettelse — ALTID inline i terminal
❌ SQL migrations (.sql filer) til labels — ALDRIG
```

---

## PÅKRÆVET (✅ = altid)

```
✅ Result<T> fra alle handlers
✅ Én .sql fil per DB-operation (embedded resource)
✅ UserId / CustomerId / ProfileId — strongly typed
✅ 0 compiler warnings efter enhver ændring
✅ Valider kun ved system-grænser
✅ Opdatér SSOT når nyt mønster opdages
✅ Labels KUN via: $labels=@(...); & scripts/localization/Add-Labels.ps1 -Labels $labels → Sync-Labels.ps1
```

---

## FEATURE-STRUKTUR

```
Features/[Domain]/[Feature]/
  [Feature]Command.cs      IRequest<Result<T>>
  [Feature]Handler.cs      AL logik
  [Feature]Validator.cs    AbstractValidator<TCommand>
  [Feature]Response.cs     output record
  [Feature]Endpoint.cs     app.MapPost(...).Map(app)
  [Feature]Page.razor      Blazor (hvis UI)
  [Feature].sql            ÉN sql per operation
```

---

## EKSTERN KODE (NeeoBovisWeb / sms-service)

```
✅ Governance-struktur og SSOT-mønstre: adoptér direkte
✅ Idéer som startpunkt: ok
❌ .cs / .razor / .sql kode: ALDRIG copy-paste uden vurdering
❌ Justificér IKKE med "NeeoBovisWeb gør det" — brug green-ai SSOT
```

---

## SSOT-MAP (topic → fil)

```
endpoint/API mønster    → docs/SSOT/backend/patterns/endpoint-pattern.md
migration/skema         → docs/SSOT/database/patterns/migration-pattern.md
auth/JWT/tenant         → docs/SSOT/identity/README.md
lokalisering/labels     → docs/SSOT/localization/label-creation-guide.md
tests/unit              → docs/SSOT/testing/patterns/unit-test-pattern.md
testing strategi        → docs/SSOT/testing/testing-strategy.md
tests/integration       → docs/SSOT/testing/patterns/http-integration-test-pattern.md
tests/automation rules  → docs/SSOT/testing/test-automation-rules.md
tests/execution         → docs/SSOT/testing/test-execution-protocol.md
debug/fejlsøgning       → docs/SSOT/testing/debug-protocol.md
doc-placering           → docs/SSOT/_system/ssot-document-placement-rules.md
governance/regler       → docs/SSOT/governance/README.md
red threads             → docs/SSOT/governance/RED_THREAD_REGISTRY.md
manglende SSOT          → docs/SSOT/governance/SSOT_GAP_PLAN.md
execution protocol      → docs/SSOT/governance/EXECUTION_PROTOCOL.md
enforcement/stop        → docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md
governance map          → docs/SSOT/governance/GOVERNANCE_MAP.md
fejl/signaler           → docs/SSOT/governance/ERROR_DETECTION.md
patterns extract        → docs/SSOT/governance/PATTERN_EXTRACTION.md
forbedringer            → docs/SSOT/governance/AUTO_IMPROVEMENT.md
udførte tasks           → docs/SSOT/governance/EXECUTION_MEMORY.md
auth flow               → docs/SSOT/identity/auth-flow.md
current user / principal→ docs/SSOT/identity/current-user.md
result pattern          → docs/SSOT/backend/patterns/result-pattern.md
blazor page pattern     → docs/SSOT/backend/patterns/blazor-page-pattern.md
e2e test pattern        → docs/SSOT/testing/patterns/e2e-test-pattern.md
tool registry/ps tools  → docs/ai-governance/tool-registry.yaml
```

---

## KOMMANDOER (copy-paste klar)

```powershell
# Byg
dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q

# Alle tests
dotnet test tests/GreenAi.Tests -v q

# Specifik test
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~<TestName>" -v n

# TRX output (til Get-FailedTestsFromTrx.ps1)
dotnet test tests/GreenAi.Tests --logger trx -v q

# Parse fejlede tests fra TRX
pwsh -File scripts/testing/Get-FailedTestsFromTrx.ps1

# ⚡ SQL validation (10x hurtigere end trial-and-error via tests)
pwsh -File scripts/debug/Test-SqlStatement.ps1 `
    -SqlFile "src/GreenAi.Api/Features/<Domain>/<Feature>/<Feature>.sql" `
    -Parameters @{ Id = 1; CustomerId = 1 }

# DB logs (debug — seneste 10 min fejl)
pwsh -File scripts/testing/Get-Latest-Errors.ps1

# DB auth-state (debug)
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" -TrustServerCertificate -Query "SELECT u.Email, p.DisplayName FROM ProfileUserMappings pum JOIN Users u ON u.Id=pum.UserId JOIN Profiles p ON p.Id=pum.ProfileId ORDER BY u.Email"

# PS modules (import i scripts)
Import-Module "$PSScriptRoot\..\modules\Core.psm1"    -Force  # timestamps, settings, connection string
Import-Module "$PSScriptRoot\..\modules\Backend.psm1" -Force  # start/stop/wait/build
Import-Module "$PSScriptRoot\..\modules\Http.psm1"    -Force  # auth + HTTP GET/POST/PUT/DELETE
Import-Module "$PSScriptRoot\..\modules\Database.psm1"-Force  # Invoke-DatabaseQuery, Test-DatabaseConnection
Import-Module "$PSScriptRoot\..\modules\Output.psm1"  -Force  # Write-Header, Write-Success, etc.
```

---

**Last Updated:** 2026-04-04

