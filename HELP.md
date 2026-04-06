# HELP — green-ai mini cockpit

> Dine mest brugte prompts og kommandoer på ét sted.

---

## 🚀 START APP

```powershell
dotnet run --project src/GreenAi.Api
# App: http://localhost:5057
```

---

## 🔨 BYGG + TEST

```powershell
# Byg (0 warnings krævet)
dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q

# Alle unit/integration tests
dotnet test tests/GreenAi.Tests -v q

# Kun governance tests (hurtig, ingen browser, ~130ms)
dotnet test tests/GreenAi.E2E --filter "Category=Governance" -v q

# Specifik test
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~SendBroadcastHandlerTests" -v n
```

---

## 🏷️ LABELS / LOKALISERING

```powershell
# ENESTE lovlige metode — inline i terminal, aldrig .ps1 fil på disk
$labels = @(
    [hashtable]@{ ResourceName = "page.myPage.Title";   ResourceValue = "Min titel";   LanguageId = 1 },
    [hashtable]@{ ResourceName = "page.myPage.Subtitle"; ResourceValue = "Min undertitel"; LanguageId = 1 }
)
& "c:\Udvikling\green-ai\scripts\localization\Add-Labels.ps1" -Labels $labels
& "c:\Udvikling\green-ai\scripts\localization\Sync-Labels.ps1"
```

❌ Aldrig SQL mod `[dbo].[Labels]`  
❌ Aldrig `.ps1` fil på disk til labels  

---

## 🗄️ DATABASE DEBUG

```powershell
# Seneste fejl (10 min)
pwsh -File scripts/testing/Get-Latest-Errors.ps1

# Seneste log-entries
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" -TrustServerCertificate `
  -Query "SELECT TOP 20 TimeStamp,Level,Message,Exception FROM Logs ORDER BY TimeStamp DESC"

# Test SQL-fil direkte (10x hurtigere end trial via tests)
pwsh -File scripts/debug/Test-SqlStatement.ps1 `
    -SqlFile "src/GreenAi.Api/Features/<Domain>/<Feature>/<Feature>.sql" `
    -Parameters @{ Id = 1; CustomerId = 1 }

# Hvad er i databasen?
Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" -TrustServerCertificate `
  -Query "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
```

---

## 📸 VISUAL AUDIT (skærmdumps til ekstern AI)

```powershell
# 1. Kør visual tests → genererer screenshots
dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~NavigationVisualTests" -v q

# 2. Pak ZIP til ekstern analyse
dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~ExportVisualAnalysisPackage" -v q
# ZIP: tests/GreenAi.E2E/TestResults/Visual/analysis-pack.zip

# 3. Upload ZIP til ChatGPT/Claude og modtag JSON-audit-respons

# Slet screenshots
Get-ChildItem tests/GreenAi.E2E/TestResults -Recurse -Filter "*.png" | Remove-Item -Force
```

---

## 💬 PROMPTS TIL AI (copy-paste klar)

### Ny feature
```
ny feature: [navn] — [hvad den gør]
```

### Ny label
```
ny label: [key] = "[dansk tekst]"
```

### Ny migration
```
ny migration: [hvad der skal til databasen]
```

### Fix test
```
test fejler: [testnavn] — [fejlbesked]
```

### CSS / UI
```
tilføj css klasse til: [komponent / hvad det skal gøre]
```

### Persist til SSOT
```
persister det vi har lavet i denne session til SSOT
```

### Visual audit
```
lav visual audit zip
```

---

## 📋 GOVERNANCE REGLER (de vigtigste)

| Regel | Hvad |
|---|---|
| ❌ EF Core | Brug Dapper + SQL-filer |
| ❌ Hardcoded hex i CSS | Brug `var(--color-*)` tokens |
| ❌ `Style="..."` på tabel-elementer | Brug `.ga-col-numeric`, `.ga-chip-reset` etc. |
| ❌ `MudTable` uden `Dense="true"` | Krav for alle tabeller |
| ❌ `outline: none` uden erstatning | Skal have `box-shadow` eller `border-color` |
| ❌ SQL INSERT mod `[dbo].[Labels]` | Kun via Add-Labels.ps1 |
| ❌ git commit/push uden godkendelse | Spørg altid først |
| ✅ `Result<T>` fra alle handlers | Altid |
| ✅ `0 compiler warnings` | Altid |

---

## 📁 SSOT MAP (hvor finder du hvad)

| Emne | Fil |
|---|---|
| Backend patterns | `docs/SSOT/backend/patterns/` |
| Database / SQL | `docs/SSOT/database/patterns/` |
| Auth / JWT | `docs/SSOT/identity/README.md` |
| Labels / lokalisering | `docs/SSOT/localization/guides/label-creation-guide.md` |
| Tests | `docs/SSOT/testing/testing-strategy.md` |
| UI / farver | `docs/SSOT/ui/color-system.md` |
| UI / komponenter | `docs/SSOT/ui/component-system.md` |
| Governance regler | `docs/SSOT/governance/README.md` |
| Seneste session-log | `docs/SSOT/governance/EXECUTION_MEMORY.md` |

---

*Opdatér denne fil når du støder på noget du vil huske.*
