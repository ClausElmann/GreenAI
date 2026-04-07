# UI Governance System — Discovery Report

> Status: Discovery only — NO implementation.  
> Dato: 2026-04-07  
> Scope: green-ai repo (`c:\Udvikling\green-ai`)

---

## 1. EXISTING SYSTEM

### 1.1 Playwright Setup

| Element | Detalje |
|---|---|
| Project | `tests/GreenAi.E2E/GreenAi.E2E.csproj` |
| Base URL | `http://localhost:5057` (app må køre manuelt) |
| Browser | Chromium (Microsoft.Playwright) |
| Headless | `GREENAI_VISUAL_HEADLESS=true` for CI; default = headful (Blazor SignalR stabilitets-fix) |
| Auth | JWT injiceres i localStorage via `SharedAuth.PrimaryAsync()` (cached, én HTTP call per process) |
| CI | Kører IKKE i CI (LocalDB unavailable) |

**Kørsel — Visual tests:**
```powershell
dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" --nologo
```

**Kørsel — Governance tests (statisk, ingen browser, ~160ms):**
```powershell
dotnet test tests/GreenAi.E2E --filter "Category=Governance" --nologo
```

**Kørsel — Export analyse-pakke (on demand):**
```powershell
dotnet test tests/GreenAi.E2E --filter ExportVisualAnalysisPackage --nologo
```

---

### 1.2 Screenshot Struktur

**Base path (relativt til test-binary):**
```
tests/GreenAi.E2E/TestResults/Visual/
  current/
    desktop/     ← 1920×1080
    laptop/      ← 1366×768
    tablet/      ← 1024×768
    mobile/      ← 390×844 (IsMobile=true)
  baseline/
    [samme mappestruktur — auto-oprettet på første kørsel]
  analysis-pack.zip  ← output fra ExportVisualAnalysisTests
  analysis-pack/     ← temp staging (slettes efter zip)
```

**Navngivning:** `{testName}.png` (sanitized med `Sanitize(callerName)`)

**Fejl-screenshots:** `{callerName}-error.png` (oprettes automatisk ved test-fejl)

**Devices** (alle 4 køres per test-metode via `ForEachDeviceAsync`):

| Navn | Width | Height | IsMobile |
|---|---|---|---|
| Desktop | 1920 | 1080 | false |
| Laptop | 1366 | 768 | false |
| Tablet | 1024 | 768 | false |
| Mobile | 390 | 844 | true |

**Baseline-opdatering:**
```powershell
$env:GREENAI_UPDATE_BASELINE = "true"
dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" --nologo
$env:GREENAI_UPDATE_BASELINE = $null
```

**Pixel-diff:** IKKE implementeret endnu — kommentar i kode: "Add SixLabors.ImageSharp when UI is stable."

---

### 1.3 CSS / Token System

**Filer i `src/GreenAi.Api/wwwroot/css/`:**

| Fil | Rolle |
|---|---|
| `design-tokens.css` | SSOT: `--color-*`, `--font-*`, `--space-*`, `--font-icon-*` |
| `app.css` | `--ga-*` aliases → `var(--color-*)`, layout tokens |
| `greenai-skin.css` | MudBlazor palette overrides |
| `greenai-enterprise.css` | Tabeller, forms, enterprise density |
| `portal-skin.css` | 43 `.ga-*` utility classes |

**Note:** `app.css` er IKKE listet i `wwwroot/css/` — den er sandsynligvis i roden af `wwwroot/` eller i `_Host.cshtml`.

**Primærfarve:** `--color-primary: #2563EB`

**MudTheme override:** Inline `<style id="greenai-palette-override">` i `Components/Layout/MainLayout.razor`

**Token-regler:**
- Hardcodede hex-farver i egne CSS-filer → governance-fejl (målt live af `CssTokenComplianceTests`)
- Allowlist for shadow-rgba-værdier og gradient() er defineret som compiled regex

---

### 1.4 Test Infrastructure

**Test-filer i `tests/GreenAi.E2E/`:**

| Fil/Mappe | Indhold |
|---|---|
| `Visual/VisualTestBase.cs` | Multi-device runner, alle layout-assertions, screenshot-helpers, auth |
| `Visual/DeviceProfile.cs` | 4 device-presets + `FolderName` helper |
| `Visual/PageVisualTests.cs` | 13 sider × 4 devices (DraftsPage, StatusPage ×3, StatusDetailPage ×2, WizardPage ×2, SelectCustomer/Profile, CustomerAdmin, AdminUserList, AdminSettings) |
| `Visual/NavigationVisualTests.cs` | UKENDT indhold — ikke læst |
| `Visual/ShowcaseVisualTests.cs` | UKENDT indhold — ikke læst |
| `ColorSystem/ColorSystemTests.cs` | Computed-CSS assertions: background, primary button, text color |
| `ColorSystem/TypographySpacingTests.cs` | Font-size (body 16px, h1 24px), line-height (24px) |
| `Governance/CssTokenComplianceTests.cs` | Statisk: scanner `.cs`/`.razor`/`.css` for hardcodede værdier — ingen browser |
| `Accessibility/AccessibilityAssertions.cs` | axe-core WCAG 2.0 A+AA — opt-in via `GREENAI_ACCESSIBILITY_GATES=true` |
| `Assertions/DesignSystemAssertions.cs` | TokensDefined, FontScale, BorderRadius, SpacingScale |
| `VisualAnalysis/VisualAnalysisExporter.cs` | Pakker screenshots + `instructions.json` i ZIP til ekstern AI-analyse |
| `VisualAnalysis/ExportVisualAnalysisTests.cs` | On-demand trigger for export |
| `E2ETestBase.cs` | Base for ikke-visuelle E2E tests (browser console capture, FailAsync, screenshot) |

**Scripts i `scripts/testing/`:**
- `Get-FailedTestsFromTrx.ps1` — parser TRX output
- `Get-Latest-Errors.ps1` — henter DB-fejl

**Scripts i roden:**
- `scripts/run-ui-autofix.ps1` — UKENDT indhold

---

### 1.5 Eksisterende DOM-Assertions i `VisualTestBase.cs`

Disse kører automatisk via `RunQualityGatesAsync` efter _enhver_ `ForEachDeviceAsync`:

| Assertion | Hvad den måler |
|---|---|
| `AssertNoHorizontalOverflowAsync` | `scrollWidth > innerWidth + 2px` |
| `AssertTopBarNotClippingContentAsync` | MudMainContent paddingTop ≥ TopBar bottom |
| `AssertNoTextOverflowAsync` | `overflow:hidden + text-overflow:ellipsis + scrollWidth > clientWidth + 4px` |
| `AssertNoVisibleErrorsAsync` | Blazor error UI, MudBlazor error alerts, error boundaries |
| `AssertTokensDefinedAsync` | 13 `--color-*` / `--ga-*` tokens defineret + korrekte hex-værdier |
| `AssertFontScaleAsync` | Ingen interaktive elementer under 12px |
| `AssertBorderRadiusAsync` | `.mud-card`, `.mud-paper` m.fl. — kun tilladte radii |
| `AssertSpacingScaleAsync` | `.mud-card`, `.hub-panel` m.fl. — kun `--ga-space-*` værdier |
| `AssertInteractiveElementsVisibleAsync` | Ingen knapper/links > 20px udenfor viewport |
| `AssertNoOverlappingClickableElementsAsync` | `elementFromPoint` — ingen clickable dækket af overlay |
| `AssertLayoutConsistencyAsync` | `el.right > vw + 2px` (first 300 elements) |
| `AssertReasonableSpacingAsync` | margin/padding > 200px, collapsed containers < 4px height |
| `AssertNavigationUsableAsync` | Nav-toggle eksisterer + overlay-nav har links hvis open |

**Note:** `AssertInteractiveElementsVisibleAsync`, `AssertNoOverlappingClickableElementsAsync`, `AssertLayoutConsistencyAsync`, `AssertReasonableSpacingAsync`, `AssertNavigationUsableAsync` er defineret i `VisualTestBase.cs` men det er UKENDT om de alle er inkluderet i `RunQualityGatesAsync` — dette er ikke verificeret.

---

## 2. GAP ANALYSIS

| Kapabilitet | Status | Note |
|---|---|---|
| **DOM metrics — horizontal overflow** | EXISTS | `AssertNoHorizontalOverflowAsync` — i `VisualTestBase.cs` |
| **DOM metrics — text overflow** | EXISTS | `AssertNoTextOverflowAsync` |
| **DOM metrics — topbar clipping** | EXISTS | `AssertTopBarNotClippingContentAsync` |
| **DOM metrics — element overlap/z-index** | EXISTS | `AssertNoOverlappingClickableElementsAsync` |
| **DOM metrics — viewport konsistens** | EXISTS | `AssertLayoutConsistencyAsync` |
| **DOM metrics — spacing anomalier** | EXISTS | `AssertReasonableSpacingAsync` |
| **DOM metrics — alignment/grid** | MISSING | Ingen assertion for x-alignment af søskendekomponenter |
| **DOM metrics — touch target size** | PARTIAL | Defineret i `RunQualityGatesAsync` comment men ukendt om aktiv |
| **Visual metrics — density/whitespace** | MISSING | Ingen ratio-beregning for whitespace vs. indhold |
| **Visual metrics — pixel-diff** | MISSING | Eksplicit noteret i kode: "not yet implemented" |
| **Token enforcement — CSS filer** | EXISTS | `CssTokenComplianceTests` — statisk scanner |
| **Token enforcement — computed runtime** | EXISTS | `AssertTokensDefinedAsync` — 13 tokens checked live |
| **Token enforcement — spacing scale** | EXISTS | `AssertSpacingScaleAsync` — checked på udvalgte selectors |
| **Token enforcement — typography scale** | EXISTS | `AssertFontScaleAsync` + `TypographySpacingTests` |
| **Token enforcement — border-radius** | EXISTS | `AssertBorderRadiusAsync` |
| **Rule engine** | PARTIAL | Assertions køres som xUnit tests — ingen ekstern rule-fil |
| **UI scoring** | MISSING | Ingen samlet score beregnes — pass/fail pr. test |
| **Cross-browser validation** | MISSING | Kun Chromium. Firefox og WebKit er IKKE konfigureret |
| **Output JSON** | PARTIAL | `instructions.json` i analyse-pakke, men ingen maskinlæsbar per-run score |
| **Fail build** | PARTIAL | Tests kan fejle build IF CI kørte dem — men CI kører dem IKKE (LocalDB) |
| **Copilot fix-datagrundlag** | PARTIAL | Screenshots + instructions.json til ekstern AI; ingen structured fix-forslag |
| **Accessibility (axe-core)** | PARTIAL | Implementeret men opt-in (`GREENAI_ACCESSIBILITY_GATES=true`) — off by default |

---

## 3. UNKNOWNS

1. **Hvad er indholdet af `RunQualityGatesAsync`?** Line 608 i `VisualTestBase.cs` — jeg har set body med `ForEachDeviceAsync` loop, men IKKE den private `RunQualityGatesAsync` metode selv. Det er uklart hvilke af de definerede assertions der faktisk aktiveres automatisk.

2. **Hvad indeholder `NavigationVisualTests.cs` og `ShowcaseVisualTests.cs`?** Kun `PageVisualTests.cs` er læst. Disse kan have yderligere sider eller patterns.

3. **Hvad gør `scripts/run-ui-autofix.ps1`?** Scriptet eksisterer i `scripts/` — er det et Copilot-fix pipeline eller manuelt? Ukendt.

4. **Er `data-testid` konsistent på alle interaktive elementer?** Assertions falder tilbage til `textContent` og `className` hvis `data-testid` mangler — det betyder at rule violations kan fejle at identificere det rigtige element.

5. **Hvilke sider HAR ikke visual test-dækning?** Pages-mapping er kun baseret på `PageVisualTests.cs` — `NavigationVisualTests.cs` og `ShowcaseVisualTests.cs` kan dække yderligere.

6. **Hvad er CSS-load-rækkefølgen i `_Host.cshtml` / `App.razor`?** `app.css` er ikke i `wwwroot/css/` — den er referenced et andet sted. Cascade-rækkefølgen er afgørende for token-override-systemet.

7. **Hvad er `data-testid='layout-root'`?** Bruges i `ColorSystemTests` — hvilken komponent emitter dette attribut? Hvis det ikke findes på alle sider fejler token-tests stille.

8. **Er `GREENAI_ACCESSIBILITY_GATES` dokumenteret som ENV var?** Den er defineret i koden men måske ikke i `AI_STATE.md` eller SSOT. Uklart for nye contributors.

9. **Hvad er tærskelværdierne for `AssertSpacingScaleAsync`?** Selectors er `.mud-card`, `.hub-panel`, `.ga-page-content` m.fl. — `hub-panel` og `ga-page-content` er projekt-specifikke; er de dokumenteret i SSOT?

10. **Eksisterer der en central routing-fil?** Blazor Router er inline i `App.razor` — der er ikke identificeret en central liste over alle routes, som govenance-systemet kan krydse mod.

---

## 4. RISKS

### Arkitektoniske risici

| Risiko | Forklaring |
|---|---|
| **Chromium-only blind spot** | Alle tests kører kun Chromium. Firefox og WebKit renderers har forskellige subpixel-rounding og CSS cascade-quirks. Token-violations på disse browsers er usynlige. |
| **LocalDB CI-blokering** | E2E tests kræver app kørende + LocalDB. CI kører dem ikke. Governance-fejl kan leve i `main` uden at blive opdaget. |
| **Pixel-diff: ingen baseline sammenligning** | Baselines gemmes men sammenlignes ikke. Visuelle regressioner fanges kun af DOM-assertions — ikke af øjenmål. |
| **`RunQualityGatesAsync` scope ukendt** | Uklart hvilke assertions der er aktive i automatisk gate. Copilot kan tro en assertion er aktiv men den er kun defined, ikke called. |
| **Score = pass/fail, ikke graderet** | Der er ingen samlet score. Copilot ved ikke om siden har "3 minor issues" vs "1 critical issue". |

### Risici for Copilot-fixes

| Risiko | Forklaring |
|---|---|
| **Selector ambiguitet** | Assertions bruger `data-testid` → `textContent` → `className` fallback. Copilot kan rette forkert element hvis `data-testid` mangler. |
| **MudBlazor skip-logik** | Mange assertions filtrerer `.mud-*` elementer fra. Det betyder at token-violations INDE i MudBlazor-komponenter ikke fanges af govenance. |
| **Element 300-grænse** | `AssertLayoutConsistencyAsync` og `AssertReasonableSpacingAsync` scanner kun de første 300 DOM-elementer. Violations på element 301+ er usynlige. |
| **Ekstern AI-afhængighed for scoring** | `VisualAnalysisExporter` genererer en ZIP til Claude/ChatGPT. Scoring er et manuelt trin — det kan ikke fail build automatisk. |
| **False positives på MudBlazor internals** | Assertions forsøger at ekskludere MudBlazor via class-matching — men et custom komponent med `mud-` i klasse-navn ville blive skipped forkert. |

---

## 5. UPDATED MINIMAL SLICE PLAN

**Revision:** Exception-model erstattet med result-objects. Scoring tilføjet. Loop-design tilføjet.

**Evidens-baseret opdatering:** `run-ui-autofix.ps1` eksisterer allerede som loop-entrypoint (max 3 iterationer, parser failures til `ui-failures.json`). Den nye plan bygger direkte videre herpå.

### Forudsætninger (uændret)
- App kørende: `http://localhost:5057`
- Én bruger authenticated: eksisterende `SharedAuth.PrimaryAsync()` genbruges
- Side: `/broadcasting` (bekræftet har `data-testid='top-bar'`, `data-testid='send-methods-grid'`)

### De 3 regler (uændrede rule-IDs)

| # | Regel | Metode | Severity |
|---|---|---|---|
| R1 | Ingen horizontal overflow | `AssertNoHorizontalOverflowAsync` | major |
| R2 | Token `--color-primary` = `#2563EB` | `AssertTokensDefinedAsync` | critical |
| R3 | Ingen tekst klippet | `AssertNoTextOverflowAsync` | minor |

### Ændringer fra v1

| Problem | v1 (fejl) | v2 (rettet) |
|---|---|---|
| Exception stopper loop | `throw Exception` → mister R3 hvis R1 fejler | try/catch wrapper → alle 3 returnerer result |
| Score 66 = meningsløs | `passCount / totalCount * 100` | weighted score pr. severity (se §6) |
| Ingen loop | kør én gang | integreres med eksisterende `run-ui-autofix.ps1` |

### Output-struktur (JSON v2)

```json
{
  "iteration": 1,
  "page": "/broadcasting",
  "device": "Desktop",
  "timestamp": "2026-04-07T14:00:00Z",
  "score": 82,
  "critical_count": 0,
  "major_count": 1,
  "minor_count": 0,
  "rules": [
    {
      "ruleKey": "layout.no_horizontal_overflow",
      "ruleId": "AssertNoHorizontalOverflow",
      "ruleVersion": "1.0",
      "shortId": "R1",
      "name": "NoHorizontalOverflow",
      "severity": "major",
      "confidence": "high",
      "passed": false,
      "message": "scrollWidth exceeds innerWidth by 14px on Desktop",
      "elements": ["\"send-methods-grid\" right=1934 (vw=1920)"],
      "selector": "[data-testid='send-methods-grid']",
      "selectorType": "data-testid",
      "executionMs": 38
    },
    {
      "ruleKey": "tokens.primary_color",
      "ruleId": "AssertTokensDefinedAsync",
      "ruleVersion": "1.0",
      "shortId": "R2",
      "name": "ColorTokenPrimary",
      "severity": "critical",
      "confidence": "high",
      "passed": true,
      "message": null,
      "elements": [],
      "selector": null,
      "selectorType": null,
      "executionMs": 12
    },
    {
      "ruleKey": "typography.no_text_overflow",
      "ruleId": "AssertNoTextOverflow",
      "ruleVersion": "1.0",
      "shortId": "R3",
      "name": "NoTextOverflow",
      "severity": "minor",
      "confidence": "low",
      "passed": true,
      "message": null,
      "elements": [],
      "selector": null,
      "selectorType": null,
      "executionMs": 55
    }
  ]
}
```

### Implementerings-plan (KUN plan — ingen kode)

1. **Ny klasse:** `GovernanceRuleResult.cs` — record med `RuleId`, `RuleName`, `Severity`, `Passed`, `Message`, `Elements[]`, `ExecutionMs`
2. **Ny klasse:** `UiGovernanceRunner.cs` — wrapper der kalder eksisterende assertions med try/catch, returnerer `List<GovernanceRuleResult>` (aldrig throw)
3. **Ny klasse:** `GovernanceScorer.cs` — beregner score fra result-liste (se §6)
4. **Ny test:** `UiGovernanceTests.cs` med `[Trait("Category", "Governance")]` — kald runner, beregn score, gem til `TestResults/governance-report.json`
5. **Integrér i `run-ui-autofix.ps1`:** Erstat regex-parsing af log med læsning af `governance-report.json` direkte

---

## 6. RESULT MODEL

### `GovernanceRuleResult` (pr. regel pr. device)

| Felt | Type | Beskrivelse |
|---|---|---|
| `ruleKey` | string | `"layout.no_horizontal_overflow"` — **STABILT semantisk nøgle** — ændres ALDRIG ved refactor |
| `ruleId` | string | `"AssertNoHorizontalOverflow"` — assertion-metode navn (intern reference) |
| `ruleVersion` | string | `"1.0"` — version af regelens semantik og vægtning |
| `shortId` | string? | `"R1"` — optional kortform til visning og logs |
| `ruleName` | string | `"NoHorizontalOverflow"` |
| `severity` | enum | `critical` / `major` / `minor` |
| `confidence` | enum | `high` / `medium` / `low` — se mapping nedenfor |
| `passed` | bool | `true` hvis assertion passerer |
| `message` | string? | `null` hvis passed; exception-message hvis failed |
| `elements` | string[] | Elementreferencer — best-effort |
| `selector` | string? | Første match der identificerede det fejlende element: `"[data-testid='send-methods-grid']"` |
| `selectorType` | enum? | `data-testid` / `semantic` / `fallback` — se regel nedenfor |
| `executionMs` | int | Tid brugt på at køre assertion |

### Severity-mapping til eksisterende assertions

| Severity | Regler | Begrundelse |
|---|---|---|
| **critical** | `AssertNoVisibleErrorsAsync`, `AssertTokensDefinedAsync` | Blocker: siden er broken eller token-system er brudt |
| **major** | `AssertNoHorizontalOverflowAsync`, `AssertTopBarNotClippingContentAsync`, `AssertNoOverlappingClickableElementsAsync`, `AssertLayoutConsistencyAsync` | Tydeligt visuel regression — brugeren oplever det |
| **minor** | `AssertNoTextOverflowAsync`, `AssertFontScaleAsync`, `AssertBorderRadiusAsync`, `AssertSpacingScaleAsync`, `AssertReasonableSpacingAsync`, `AssertNavigationUsableAsync`, `AssertInteractiveElementsVisibleAsync` | Design-system afvigelse — ikke umiddelbart funktionelt blokerende |

### Indsamlingsstrategi

Eksisterende assertions kaster `Exception` med struktureret besked.  
Wrap-pattern:

```
foreach regel:
  start timer
  try { await AssertX(page, device) }
  catch (Exception ex) { result.passed = false; result.message = ex.Message; result.elements = TryParseElements(ex.Message) }
  finally { result.executionMs = elapsed }
```

**Ændring fra v2 — Element parsing er OPTIONAL, ikke PRIMARY:**

| Felt | Status | Copilot-afhængighed |
|---|---|---|
| `message` | **PRIMARY** — altid til stede | Copilot MÅ stole på dette |
| `elements` | **OPTIONAL** — best-effort | Copilot MÅ IKKE stole på dette |

`TryParseElements()` forsøger at ekstrahere `"..."` strenge fra exception-besked — men returnerer tom liste ved parse-fejl, aldrig throw.  
Exception-formater er interne implementeringsdetaljer — de kan ændre sig.  
`message`-feltet er stabilt fordi det er den rå exception-tekst.

### `ruleKey` Mapping (STABIL API — ændres ALDRIG)

`ruleKey` er den offentlige, stabile identifikator. `ruleId` (metode-navn) må refactores frit — `ruleKey` må ALDRIG ændres uden `ruleVersion` bump.

| ruleKey | ruleId | ruleVersion |
|---|---|---|
| `layout.no_horizontal_overflow` | `AssertNoHorizontalOverflowAsync` | 1.0 |
| `tokens.primary_color` | `AssertTokensDefinedAsync` | 1.0 |
| `typography.no_text_overflow` | `AssertNoTextOverflowAsync` | 1.0 |
| `component.no_visible_errors` | `AssertNoVisibleErrorsAsync` | 1.0 |
| `layout.topbar_not_clipping` | `AssertTopBarNotClippingContentAsync` | 1.0 |
| `z-index.no_overlapping_clickable` | `AssertNoOverlappingClickableElementsAsync` | 1.0 |
| `layout.spacing_scale` | `AssertSpacingScaleAsync` | 1.0 |
| `typography.font_scale` | `AssertFontScaleAsync` | 1.0 |
| `tokens.border_radius` | `AssertBorderRadiusAsync` | 1.0 |
| `layout.consistency` | `AssertLayoutConsistencyAsync` | 1.0 |
| `layout.reasonable_spacing` | `AssertReasonableSpacingAsync` | 1.0 |
| `component.navigation_usable` | `AssertNavigationUsableAsync` | 1.0 |
| `layout.interactive_elements_visible` | `AssertInteractiveElementsVisibleAsync` | 1.0 |

**Copilot og scripts bruger KUN `ruleKey`** — aldrig `ruleId` direkte. `ruleId` er intern implementation.

### `selectorType` og fix-autorisation

Når en assertion fejler, gemmes den selector der identificerede det fejlende element:

| selectorType | Hvad det betyder | Copilot-handling |
|---|---|---|
| `data-testid` | Eksakt match på `data-testid` attribut | **AUTO-FIX TILLADT** |
| `semantic` | Match på semantisk HTML (`table`, `form`, `[role="dialog"]`) | Fix med forsigtighed — verificer før |
| `fallback` | Match på className / textContent | **REPORT ONLY — DO NOT AUTO-FIX** |
| `null` | Intet element identificeret | **REPORT ONLY — DO NOT AUTO-FIX** |

**Begrundelse:** `fallback`-baserede fixes er non-deterministic — Copilot kan ramme forkert element og introducere UI drift. Stop er altid bedre end gæt.

`suggestedArea` afledes automatisk fra `ruleKey` prefix: `layout.no_horizontal_overflow` → `layout`.  
Sættes i `UiGovernanceRunner` — aldrig manuelt, aldrig af Copilot.

**Fallback — ukendt ruleKey:**

```
if ruleKey not in ruleKeyMapping:
  suggestedArea = "unknown"
```

**Copilot-regel:** `suggestedArea == "unknown"` → **STOP. DO NOT FIX. REPORT:**  
`"Ukendt regel: {ruleKey} — ingen mapping — kræver manuel review"`

### Confidence Signal

`confidence` afspejler præcisionen af assertion-teknikken — uafhængig af `severity` (vigtighed).

| Confidence | Assertions | Begrundelse |
|---|---|---|
| **high** | `AssertNoVisibleErrorsAsync`, `AssertTokensDefinedAsync`, `AssertNoHorizontalOverflowAsync`, `AssertTopBarNotClippingContentAsync`, `AssertNoOverlappingClickableElementsAsync`, `AssertLayoutConsistencyAsync` | Eksakte pixel/token-checks — ingen heuristik |
| **medium** | `AssertSpacingScaleAsync`, `AssertReasonableSpacingAsync`, `AssertInteractiveElementsVisibleAsync` | Selector-baserede checks — kan misse edge cases |
| **low** | `AssertFontScaleAsync`, `AssertBorderRadiusAsync`, `AssertNavigationUsableAsync`, `AssertNoTextOverflowAsync` | Heuristisk / threshold-baseret — false positives mulige |

**Copilot prioritering:**

| Confidence | Copilot-handling |
|---|---|
| `high` | Fix med fuld tillid — fejl er definitiv |
| `medium` | Fix med forsigtighed — verificer at fix ikke bryder andre selectors |
| `low` | Fix KUN hvis message er entydig. Hvis tvivl → REPORT, ikke fix |

**Bemærk:** `confidence` og `severity` er ortogonale.  
Criticals er altid `high` confidence. `low` confidence forekommer primært som `minor` severity.  
En `low`-confidence assertion fejler sjældnere — men når den fejler er fix-retningen usikker.

---

## 7. SCORING MODEL

### Vægter

| Severity | Pointværdi per regel | Begrundelse |
|---|---|---|
| critical | 50 | Blocker — stopper systemet korrekt, men ikke uigenopliveligt |
| major | 15 | Klar regression — vigtig men ikke fatal |
| minor | 5 | Design-hygiejne |

**Ændring fra v2:** critical sænket fra 40→50, major sænket fra 20→15.  
Ratioen major/minor er bibeholdt (3:1). Critical er now dedikeret stopper — ikke score-drænende.

### Score-formel

```
maxScore = Σ(weight_i for all rules)
earnedScore = Σ(weight_i for passed rules)
score = round(earnedScore / maxScore × 100)
```

**Eksempel** med 2 critical + 4 major + 7 minor rules:

```
maxScore = (2×50) + (4×15) + (7×5) = 100 + 60 + 35 = 195
alle passed → score = 100

1 critical failed: earnedScore = 145 → score = 74
1 major failed:    earnedScore = 180 → score = 92
1 minor failed:    earnedScore = 190 → score = 97
```

**Bemærk:** Samme total (195) som v2, men different distribution — critical-fejl rammer hårdere (100→145 vs 80→155).

### `GovernanceScore` objekt

| Felt | Type | Eksempel |
|---|---|---|
| `score` | int (0–100) | `79` |
| `maxScore` | int | `195` |
| `earnedScore` | int | `155` |
| `criticalCount` | int | `1` |
| `majorCount` | int | `0` |
| `minorCount` | int | `0` |
| `passThreshold` | int | `80` (konfigurerbar) |
| `passed` | bool | `false` (score < threshold) |
| `dimensions` | object | Per-kategori score — se nedenfor |

### Score-dimensioner

`dimensions` brækker den samlede score ned per `suggestedArea`-kategori. Giver Copilot præcis retning frem for bare "score = 79".

```json
"dimensions": {
  "layout":     0.92,
  "tokens":     1.00,
  "typography": 0.85,
  "z-index":    1.00,
  "component":  0.67
}
```

**Beregning:** `dimensions[category] = earnedScore[category] / maxScore[category]`  
Kategori afledes fra `ruleKey` prefix (f.eks. `layout.no_horizontal_overflow` → `layout`).

**Copilot-brug:** De laveste dimensioner angiver prioriteringsorden når `severity` er ens.

**Eksempel:** `component: 0.67` + `AssertNoVisibleErrorsAsync` fejlet → fix `.razor`-komponent før CSS.

### Threshold for fail-build

```
if (score < 80) → fail
if (criticalCount > 0 AND iteration >= 2) → fail
if (criticalCount > 0 AND iteration == 1) → WARN, fortsæt loop (Copilot må fixe)
```

**Ændring fra v2:** `criticalCount > 0` er IKKE længere altid-fail.  
Iteration 1 med criticals → Copilot får chance. Iteration 2+ med criticals → hård stop.  

**Begrundelse:** Systemet skal kunne iterere. En critical på iteration 1 er en opgave, ikke en dom.  
En critical på iteration 2 er bevis på at Copilot ikke har løst den — stop er korrekt her.

**Evidens:** `run-ui-autofix.ps1` afslutter allerede med `exit 1` ved failures. Iteration-tæller er allerede tilgængelig i scriptet (`$i`).

---

## 8. LOOP DESIGN (ITERATION MODEL)

### Eksisterende fundament

`run-ui-autofix.ps1` implementerer allerede:
- `MaxIterations = 3` loop
- `ui-failures.json` output med type + suggestedFix
- `exit 0` ved success, `exit 1` ved max-iterations nået

**Problem med nuværende implementation:** Failures parses fra plaintext log med regex (`-match "color-contrast"`). Det er skrøbeligt og mister struktureret element-info.

**Løsning:** Replace regex-parsing med læsning af `governance-report.json`.

### Iteration-flow (opdateret)

```
run-ui-autofix.ps1 startes (MaxIterations = 3)
│
├── Iteration N
│   ├── 1. Kør: dotnet test --filter "Category=UIGovernance"
│   │          → skriver TestResults/governance-report.json
│   │
│   ├── 2. Læs governance-report.json
│   │
│   ├── 3. score ≥ threshold OG criticalCount = 0?
│   │   └── JA → exit 0 (done)
│   │
│   ├── 4. Transformer governance-report.json → copilot-fix-input.json
│   │       format: { priority: sorted by severity, element: "...", suggestedArea: "css|razor|token" }
│   │
│   ├── 5. Copilot læser copilot-fix-input.json
│   │       → fix TOP 1 critical ELLER TOP 3 major af samme type
│   │       → aldrig fix minor i samme iteration som critical
│   │
│   └── N++
│
└── Iteration > MaxIterations → exit 1, bevar governance-report.json til manuel review
```

### `copilot-fix-input.json` format (input til Copilot pr. iteration)

```json
{
  "iteration": 2,
  "score": 79,
  "fixTarget": "TOP_CRITICAL_FIRST",
  "fixes": [
    {
      "priority": 1,
      "ruleId": "R2",
      "severity": "critical",
      "message": "MISSING: --color-primary (expected #2563EB)",
      "elements": [],
      "suggestedArea": "css",
      "hint": "Verify design-tokens.css is loaded before greenai-skin.css"
    }
  ]
}
```

### Delta-tracking (pr. iteration)

```json
// TestResults/governance-delta.json
{
  "fromIteration": 1,
  "toIteration": 2,
  "fixed": ["R2"],
  "newFailures": [],
  "unchanged": ["R1"],
  "scoreDelta": +18
}
```

**Copilot-regel:** Hvis `newFailures` er ikke-tom i delta → Copilot har introduceret en regression → stop loop, report.

### Safety Brake — Regression Guard

```
if (scoreDelta < 0):
  STOP loop
  skriv til governance-delta.json: { reason: "regression", scoreDelta: N }
  exit 1 med besked: "Score FELL by N points — regression detected, manual review required"
```

**Begrundelse:** Uden safety brake kan Copilot fixe R1, bryde R4, og loopen fortsætter blindt.  
Self-inflicted regressions er det farligste failure mode i AI-loop-systemer.

**Threshold:** `scoreDelta < 0` er hård stop. `scoreDelta = 0` (ingen fremgang) tæller som iteration — men se Stuck Detection nedenfor.

### Stuck Detection — No-Progress Guard

```
if (scoreDelta == 0 for 2 consecutive iterations):
  STOP loop
  skriv til governance-delta.json: { reason: "no-progress", stuckAtScore: N, iterations: [i-1, i] }
  exit 1 med besked: "No score improvement for 2 iterations — Copilot is stuck, manual review required"
```

**Begrundelse:** `scoreDelta < 0` fanger regressions, men ikke fastlåste loops.  
Copilot kan gentage samme (forkerte) fix i 3 iterationer og ramme MaxIterations fejlagtigt frem for at stoppe selv.  
No-progress guard stopper 2 iterationer tidligere og giver en præcis fejlbesked.

**Consecutive-tracking:** `governance-delta.json` gemmer `scoreDelta` pr. iteration. Stuck detection kræver kun de 2 seneste deltas.

### Batching-regler for Copilot

| Situation | Copilot-handling |
|---|---|
| criticalCount > 0 | Fix kun criticals. Ignorer major/minor denne iteration. |
| criticalCount = 0, majorCount > 0 | Fix op til 3 majors af samme `suggestedArea` |
| criticalCount = 0, majorCount = 0 | Fix op til 5 minors |
| `newFailures` > 0 efter fix | Stop. Skriv til delta. Vent på bruger. |

### Blast Radius Control

**Copilot må KUN ændre én `suggestedArea`-kategori pr. iteration.**

```
copilot-fix-input.json indeholder:
  "allowedCategory": "layout"  ← systemet sætter dette baseret på prioritering

Copilot-regel:
  Ændr KUN filer relateret til category == allowedCategory
  Pr. iteration: én kategori, én ændring ad gangen
```

**Kategori → fil-type mapping:**

| Kategori | Tilladt fil-type | Eksempel |
|---|---|---|
| `layout` | `.css` | `greenai-enterprise.css`, `portal-skin.css` |
| `tokens` | `.css` | `design-tokens.css`, `app.css` |
| `typography` | `.css` | `greenai-enterprise.css` |
| `z-index` | `.css` | `greenai-skin.css` |
| `component` | `.razor` | Blazor-komponenter i `Components/` |

**Begrundelse:** Et CSS layout-fix kan cascade til spacing, typography og z-index.  
Én kategori pr. iteration + regression guard = maksimal kontrol over afledte effekter.

**Effekt:** En Copilot der fixer `layout` én iteration og `typography` næste iteration er forudsigelig.  
En Copilot der fixer begge i én operation er ikke.

### Problem

- `FullPage = true` kan producere 3000–6000px høje billeder
- Ekstern AI (ChatGPT/Claude vision API) har ~2000px max dimension
- Lange screenshots kollapser detaljer i thumbnail-view

### Eksisterende evidens

`VisualAnalysisExporter.cs` pakker allerede screenshots i ZIP til ekstern AI — men bruger de eksisterende full-page screenshots ufiltrerede.

### Strategi: 3-segment capture

For sider der er længere end viewport-height: tag 3 overlappende viewport-snapshots i stedet for ét full-page screenshot.

| Segment | Navn | Scroll-position | Hvad det viser |
|---|---|---|---|
| fold | `{testName}-fold.png` | top (0) | Above fold / primary action |
| mid | `{testName}-mid.png` | 50% af scroll-height | Midt-sektion |
| bottom | `{testName}-bottom.png` | max scroll | Footer / pagination / action-bar |

**Max dimension:** Viewport-height = 1080px (Desktop), 768px (Laptop/Tablet), 844px (Mobile) — alle under 2000px.

**Implementerings-princip (KUN plan):** Erstat `FullPage = true` med 3 kald til `page.EvaluateAsync` (scroll) + `page.ScreenshotAsync` (viewport-only). Basis-logik:

```
1. Mål scroll-height: page.EvaluateAsync<int>("() => document.body.scrollHeight")
2. Hvis scrollHeight ≤ viewport * 1.2 → tag ét screenshot (viewport-only)
3. Hvis scrollHeight > viewport * 1.2 → tag 3 segmenter:
     fold:   scroll(0),    screenshot
     mid:    scroll(50%),  screenshot
     bottom: scroll(100%), screenshot
```

### Navngivning (extended)

| Situation | Filnavn | Max px |
|---|---|---|
| Kort side (≤ 1.2× viewport) | `{testName}.png` | ~1080px |
| Lang side — fold | `{testName}-fold.png` | 1080px |
| Lang side — mid | `{testName}-mid.png` | 1080px |
| Lang side — bottom | `{testName}-bottom.png` | 1080px |
| Fejl-screenshot | `{testName}-error.png` | viewport-only |

### ZIP-pakke til ekstern AI (opdateret)

`VisualAnalysisExporter` skal inkludere segmenter i struktureret mappe:

```
analysis-pack.zip
├── desktop/
│   ├── broadcasting-hub-fold.png
│   ├── broadcasting-hub-mid.png
│   └── broadcasting-hub-bottom.png
├── mobile/
│   └── broadcasting-hub.png       ← kort nok til ét screenshot
└── instructions.json
```

### Undtagelser

- Mobile (390×844): De fleste sider er under 1.2× viewport — ét screenshot er nok
- Auth-sider (login, select-customer): Korte sider — altid ét screenshot
- Dialog-screenshots: Altid viewport-only (dialog er centered i viewport)

### Component Focus Shots (tilføjet v3)

**Begrundelse:** Enterprise UI-issues lever i komponenter — ikke i full-page view. En tabel-overflow eller dialog-spacing-fejl er usynlig i et 1920×3000px screenshot.

| Komponent-type | Trigger | Filnavn | Selector |
|---|---|---|---|
| Dialog / modal | Åben state | `{testName}-dialog.png` | Se selector-strategi nedenfor |
| Primær form | Altid (hvis til stede) | `{testName}-form.png` | Se selector-strategi nedenfor |
| Datatabel | Altid (hvis til stede) | `{testName}-table.png` | Se selector-strategi nedenfor |
| Error state | Fejl-screenshot path | `{testName}-error-focus.png` | `.mud-alert-filled-error` |

### Selector-strategi for focus shots (4-trin fallback)

`data-testid` er ikke konsistent i grøn-ai. Brug altid denne prioriterede rækkefølge:

| Prioritet | Strategi | Eksempel |
|---|---|---|
| 1 | `data-testid` | `[data-testid*="table"]`, `[data-testid*="form"]` |
| 2 | Semantisk HTML | `table`, `form`, `[role="dialog"]` |
| 3 | MudBlazor klasser | `.mud-table`, `.mud-dialog`, `.mud-form` |
| 4 | Skip | Komponent ikke fundet → ingen focus shot, ingen fejl |

**Regel:** Gå altid trin 1→2→3→4. Stop ved første match. Trin 4 er aldrig en fejl — det er en no-op.

**Capture-strategi for focus shots:**

```
1. Forsøg selector trin 1–3 (første match vinder)
2. Hvis ingen match → skip (ingen fejl, ingen log)
3. ScrollIntoView(komponent)
4. Vent 100ms (CSS settle)
5. Tag viewport screenshot (IKKE full-page)
6. Gem til current/{device}/{testName}-{komponenttype}.png
```

**Vigtigt:** Focus shots er SUPPLEMENT — ikke erstatning for segment-captures.  
Segment-captures viser layout. Focus shots viser komponent-detaljer.

### State Captures (tilføjet v5)

**Begrundelse:** Segment-captures og focus shots dækker _layout_ og _komponenter_.  
Men de viser kun ét state — den tilstand siden tilfældigvis er i når testen kører.  
Enterprise UI har 4 distinkte states der alle kan have egne visuelle fejl.

| State | Filnavn | Trigger | Hvad det fanger |
|---|---|---|---|
| loading | `{testName}-loading.png` | Screenshot tages *før* data loader (Skeleton/spinner synlig) | Skeleton layout, spinner placering, tomme tabeller |
| empty | `{testName}-empty.png` | Screenshot tages når liste/tabel er tom (0 rækker) | Empty state UI, call-to-action placering |
| error | `{testName}-error.png` | Screenshot tages når API returnerer fejl / alert synlig | Error message layout, alert placering, recovery UI |
| success | `{testName}-success.png` | Screenshot tages når data er loaded og synlig | Normal fuldt loaded state |

**Capture timing:**

```
loading:  screenshot tages i samme tick som navigation (before await networkidle)
empty:    inject tom response → wait for render → screenshot
error:    inject error response → wait for alert → screenshot
success:  standard screenshot (= nuværende behavior)
```

**Eksempler:**

```
broadcasting-hub-loading.png   ← skeleton/spinner synlig
broadcasting-hub-empty.png     ← ingen SMS-jobs endnu
broadcasting-hub-error.png     ← API 500 → alert
broadcasting-hub-success.png   ← 5 jobs loaded
```

**Implementerings-bemærkning (KUN plan):** `loading`-state kræver timing-kontrol (screenshot *under* load).  
`empty` og `error` states kræver mock/inject strategi — ikke implementeret endnu.  
`success` er gratis (= nuværende screenshot).

**Prioritering:** `success` er allerede implementeret. `error` har højest værdi næst (fanger UI-fejl der kun ses ved API-fejl). `loading` og `empty` er nice-to-have.

### Opdateret ZIP-struktur med state captures

```
analysis-pack.zip
├── desktop/
│   ├── broadcasting-hub-fold.png          ← segment: above fold
│   ├── broadcasting-hub-mid.png           ← segment: midt
│   ├── broadcasting-hub-bottom.png        ← segment: bund
│   ├── broadcasting-hub-table.png         ← focus: broadcast-tabel
│   ├── broadcasting-hub-form.png          ← focus: send-form (hvis til stede)
│   ├── broadcasting-hub-loading.png       ← state: skeleton/spinner
│   ├── broadcasting-hub-empty.png         ← state: ingen data
│   └── broadcasting-hub-error.png         ← state: API-fejl
├── mobile/
│   ├── broadcasting-hub.png               ← enkelt segment (kort nok)
│   └── broadcasting-hub-table.png         ← focus: tabel på mobile
└── instructions.json
```

---

*Sidst opdateret: 2026-04-07 (v7 — system binding, rule engine, regression detection, state coverage, cross-browser, end-to-end diagram)*

---

## 16. UI TARGET CONFIG — `ui_targets.json`

Styrer hvilke pages, states og komponenter governance-systemet tester. Uden denne fil er page-selection hardcodet i tests.

**Fil:** `tests/GreenAi.E2E/Governance/ui_targets.json`

**Struktur:**

```json
{
  "pages": [
    {
      "route": "/broadcasting",
      "label": "broadcasting-hub",
      "priority": "high",
      "devices": ["desktop", "mobile"],
      "states": ["success", "error"],
      "components": ["table", "form"]
    }
  ]
}
```

**Felter:**

| Felt | Semantik |
|---|---|
| `priority` | `high` = altid kør; `low` = skip i fast-mode |
| `devices` | Tilsidesætter global 4-device standard pr. side |
| `states` | Påkrævede state captures (success, error, empty, loading) |
| `components` | Påkrævede focus shots (table, form, dialog) |

**Ejerforhold:**
- `UiGovernanceRunner` læser filen for page-liste
- `run-ui-autofix.ps1` bruger `priority`-felt til at scope til `high`-sider i hurtig-iteration
- Tilføj side = én linje i JSON — ingen kode-ændring

---

## 17. FIX STRATEGY

**Problem:** `copilot-fix-input.json` angiver WHERE — ikke HOW. Copilot gætter strategi fra `message`-tekst (non-deterministic).

**Nyt felt på `GovernanceRuleResult`:**

| Felt | Type | Hvem sætter det |
|---|---|---|
| `fixStrategy` | string \| null | `UiGovernanceRunner` via `ruleKey`-opslag |

**Katalog:**

| `fixStrategy` | Trigger-regel | Copilot-handling |
|---|---|---|
| `replace-hardcoded-color-with-token` | `tokens.*` | Find hex i CSS → erstat med `var(--color-*)` |
| `add-overflow-constraint` | `layout.no_horizontal_overflow` | Find element → tilføj `overflow:hidden` eller `max-width:100%` |
| `fix-z-index-stacking` | `z-index.*` | Find overlay → justér z-index relativt til sibling |
| `reduce-spacing-value` | `layout.reasonable_spacing` | Find margin/padding >200px → erstat med `--ga-space-*` token |
| `add-min-font-size` | `typography.font_scale` | Find element <12px → fjern `font-size` override |
| `fix-component-error-state` | `component.no_visible_errors` | Debug Blazor komponent — ikke CSS |
| `add-browser-specific-override` | `meta.cross_browser_inconsistency` | Tilføj `@supports`-override (se §18) |
| `report-only` | `confidence=low` ELLER `selectorType=fallback` | Ingen fil-ændring |

**Copilot-regel:** `fixStrategy` er altid input — aldrig valgt af Copilot. Manglende eller `null` = behandl som `report-only`.

---

## 18. CROSS-BROWSER CONSISTENCY RULE

**Meta-regel** der kører POST browser-runs — ikke inde i dem.

**Logik:** Sammenlign `ruleKey + device` resultater på tværs af `governance-report-chromium.json`, `governance-report-firefox.json`, `governance-report-webkit.json`.

**Failure-betingelse:** `chromium.passed = true` OG (`firefox.passed = false` ELLER `webkit.passed = false`) for samme `ruleKey + device`.

**Result fields for meta-reglen:**

| Felt | Værdi |
|---|---|
| `ruleKey` | `meta.cross_browser_inconsistency` |
| `severity` | `major` |
| `confidence` | `high` |
| `selectorType` | `null` |
| `suggestedArea` | `css` |
| `fixStrategy` | `add-browser-specific-override` |
| `message` | `"[ruleKey]: pass/chromium, fail/firefox, device=desktop"` |

**Undtagelse fra `selectorType=null → REPORT ONLY`-reglen:**
`fixStrategy = add-browser-specific-override` er tilladt selv uden selector. Browser-CSS-override kræver ikke DOM-element — det er et `@supports`/media-level fix. Alle andre `null`-selector fixes forbliver REPORT ONLY.

**Browser-separation:** Rapporter gemmes per browser (`governance-report-{browser}.json`). Meta-reglen er det eneste sted de sammenlignes. Chromium-rapporten er fortsat primær — Firefox og WebKit kører kun browser-sensitive rules (jf. §14).

---

## 19. COMPONENT OWNERSHIP

**Problem:** `suggestedArea = "layout"` angiver kategori — ikke fil.

**Ny fil:** `tests/GreenAi.E2E/Governance/component-ownership.json`

**Felter pr. ruleKey:**

| Felt | Semantik |
|---|---|
| `area` | = `suggestedArea` |
| `likely_files` | CSS-filer der historisk ejer domænet (liste) |
| `blazor_component` | Specifik `.razor`-fil hvis komponent-specifik |
| `note` | Kort root-cause-hint til Copilot |

**Eksempel-entries:**

| ruleKey | likely_files | blazor_component |
|---|---|---|
| `layout.no_horizontal_overflow` | `greenai-enterprise.css`, `portal-skin.css` | null |
| `tokens.primary_color` | `design-tokens.css`, `app.css` | null |
| `component.no_visible_errors` | null | `Components/Layout/MainLayout.razor` |
| `z-index.no_overlapping_clickable` | `greenai-skin.css` | null |

**Brug i loop:**
`run-ui-autofix.ps1` merger `copilot-fix-input.json` med ownership-lookup. Copilot modtager `likely_files` + `blazor_component` — aldrig en direkte fil-edit-kommando.

**Fallback:** Ingen match i `component-ownership.json` → `fixStrategy = report-only` uanset `selectorType`.

**Maintenance:** Opdateres manuelt efter historisk fix-analyse. Initial entries afledt fra `ruleKey`-mapping i §6. Det er et levende dokument — ikke statisk.

---

## 20. CONFIDENCE ENFORCEMENT — STRICT MATRIX

| Confidence | selectorType | Action |
|---|---|---|
| `high` | `data-testid` | **AUTO-FIX** |
| `high` | `semantic` | **FIX MED GUARD** |
| `high` | `fallback` \| `null` | **REPORT — høj prioritet i fix-kø** |
| `medium` | `data-testid` | **FIX MED GUARD** |
| `medium` | `semantic` | **REPORT ONLY** |
| `medium` | `fallback` \| `null` | **REPORT ONLY** |
| `low` | `data-testid` + klar message | **FIX KUN HVIS entydigt** |
| `low` | `data-testid` + uklar message | **ESCALATE** |
| `low` | `semantic` \| `fallback` \| `null` | **ESCALATE** altid |

**FIX MED GUARD:** Fix udføres, men `pendingVerification: true` skrives til `governance-delta.json`. Næste iterations delta-check verificerer at tilstødende rules ikke brød.

**ESCALATE:** Hverken fix eller passiv report — aktiv bloker der kræver bruger-input. Skrives til `governance-escalations.json`.

**Nøgle-distinktioner:**
- `high + fallback` = REPORT MEN IKKE ESCALATE (Copilot ansvarsfri — men høj prioritet)
- `low + data-testid + uklar message` = ESCALATE (message er eneste navigations-signal — uklart = ingen valid fix-path)
- `medium + data-testid` = FIX MED GUARD (ikke blind auto-fix — verificer delta)

---

## 21. UI FREEZE MODE

**Definition:** Systemet "freezer" når score er stabil og høj — forhindrer utilsigtede regressioner under løbende feature-arbejde.

**Aktiverings-betingelse:**
Score ≥ 80 AND criticalCount = 0 for 3 på hinanden følgende runs UDEN kode-ændringer.

**Output:** `governance-freeze.json` — eksistens = freeze aktiv, indhold = `{ frozenAt: score, frozenSince: timestamp }`.

**Gates under freeze (pre-commit check):**

| Ændring | Gate |
|---|---|
| CSS i `layout`/`tokens`/`typography`-filer | Governance re-run krævet INDEN commit |
| `.razor`-komponent i `Components/`-mappe | Governance re-run krævet INDEN commit |
| Ny side i `ui_targets.json` | Governance re-run krævet for ny side |
| Testfil-ændring | Ingen krav |

**Gate-mekanisme:**
Commit er tilladt hvis: `governance-report.json` timestamp > `governance-freeze.json` timestamp AND score ≥ 80. Ellers BLOCK.

**Unfreeze:** Score falder under 80 → `governance-freeze.json` slettes automatisk.

**Effekt:** Governance uden freeze = reaktivt (rapportér fejl efter). Med freeze = præventivt (blokér fejl inden de opstår).

---

## 22. ESCALATION FLOW

**To separate escalation paths:**

### Path A — `selectorType = fallback`

```
→ fixStrategy = report-only
→ skriv til: governance-escalations.json (reason: "no-reliable-selector")
→ EKSKLUDÉR fra copilot-fix-input.json fix-liste
→ score: tæller som "unresolvable" — ikke Copilots ansvar — angives som unresolvedCount
→ loop: FORTSÆTTER (bloker ikke)
→ suggestedHumanAction: "add-data-testid-to-element"
```

### Path B — `suggestedArea = unknown`

```
→ fixStrategy = null (hardstop)
→ skriv til: governance-escalations.json (reason: "no-ownership-mapping")
→ copilot-fix-input.json: tilføj med fixStrategy = "escalate-to-human"
→ loop: STOPPER for denne ruleKey + inkrement "escalatedRules"-tæller
→ escalatedRules > 2 i samme iteration → exit 1
→ suggestedHumanAction: "add-rule-to-component-ownership"
```

**Forskel:**

| | Path A | Path B |
|---|---|---|
| Loop-effekt | Ingen — fortsætter | Stop for regel + tæller mod exit 1 |
| Score-ansvar | Copilot IKKE ansvarlig | Copilot ansvarlig (tæller som failed) |
| Root cause | Manglende `data-testid` | Manglende entry i `component-ownership.json` |
| Handling | Akkumulér til rapport | Hard stop → bruger-input |

**Eskalations-output:**
`governance-escalations.json` er det eneste output Copilot ALDRIG kan resolve autonomt. Det er input til næste manuelle session — ikke til næste iteration.

---

## 23. RULE VERSION GOVERNANCE

**Problem:** `ruleVersion = "1.0"` er sat på alle rules — men bruges ikke til noget. Hvis threshold i `AssertNoHorizontalOverflowAsync` ændres fra 2px → 4px, brekker historisk score-sammenligning.

**Regel:** `scoreDelta` og regression-detection er kun gyldige hvis `ruleVersion` matcher mellem to runs.

**Implementering i `governance-delta.json`:**

```json
"versionMismatch": [
  {
    "ruleKey": "layout.no_horizontal_overflow",
    "prevVersion": "1.0",
    "currVersion": "2.0"
  }
]
```

**Konsekvens af mismatch:**
- `scoreDelta` for den pågældende `ruleKey` ignoreres (tæller ikke mod regression eller stuck)
- `newFailures` ekskluderer reglen fra iterations-delta
- Logges som `"reason": "version-changed"` — ikke en fejl, ikke en regression

**Change log:**
Enhver `ruleVersion`-bump kræver entry i `tests/GreenAi.E2E/Governance/rule-changelog.json`:

```json
{
  "ruleKey": "layout.no_horizontal_overflow",
  "from": "1.0",
  "to": "2.0",
  "change": "threshold 2px → 4px",
  "date": "2026-04-07"
}
```

---

## 24. RUN SIGNATURE

**Problem:** `governance-report.json` har ingen identitet. Det er umuligt at vide hvilken kode, hvilken config og hvilken commit der producerede en given score.

**Nyt top-level felt i `governance-report.json`:**

```json
"runSignature": {
  "gitCommit": "abc1234",
  "gitBranch": "main",
  "uiTargetsHash": "sha256:...",
  "rulesHash": "sha256:...",
  "envMode": "full",
  "timestamp": "2026-04-07T14:00:00Z"
}
```

**Felter:**

| Felt | Kilde | Formål |
|---|---|---|
| `gitCommit` | `git rev-parse --short HEAD` | Reproducerbarhed |
| `gitBranch` | `git rev-parse --abbrev-ref HEAD` | Kontekst |
| `uiTargetsHash` | SHA256 af `ui_targets.json` | Detect config-ændring |
| `rulesHash` | SHA256 af `component-ownership.json` | Detect ownership-ændring |
| `envMode` | Fra `GREENAI_GOVERNANCE_MODE` ENV var | Sammenlign kun runs med samme mode |

**Delta-validering:** Regression-detection AFVISES hvis `uiTargetsHash` eller `rulesHash` ændrede sig mellem forrige og nuværende run. Config-ændring er ikke en regression — det er en ny baseline.

---

## 25. PARTIAL SUCCESS LOGIK

**Problem:** 1 uløst critical + 20 rettede majors → systemet rapporterer stadig FAIL. Copilot og bruger ser ikke fremgang.

**Nyt felt `progress` i `governance-report.json`:**

```json
"progress": {
  "fixedThisRun": 4,
  "remainingCritical": 1,
  "remainingMajor": 0,
  "remainingMinor": 2,
  "trend": "improving",
  "projectedPassIteration": 3
}
```

**`trend`-værdier:**

| Værdi | Betingelse |
|---|---|
| `improving` | `scoreDelta > 0` |
| `stagnant` | `scoreDelta = 0` |
| `regressing` | `scoreDelta < 0` |
| `converging` | `remainingCritical = 0` AND `score < 80` |

**`projectedPassIteration`:** Simpel lineær projektion — `(80 - currentScore) / avgScoreDeltaPerIteration`. Afrundet op. Vises som hint, ikke garanti.

**Loop-integration:** Stuck-detection (§8) bruger stadig `scoreDelta = 0 × 2` som stop. `trend = improving` ophæver ikke stuck-stop — det er supplement til diagnostik, ikke ændring af loop-logik.

---

## 26. TIME DIMENSION — HISTORIK

**Problem:** `governance-delta.json` gemmer kun forrige → nuværende iteration. Trend over tid — stagnation, drift, langsigtet forbedring — er usynlig.

**Ny fil:** `TestResults/governance-history.json` — append-only, aldrig overskrevet.

**Struktur:**

```json
[
  { "run": 1, "timestamp": "...", "gitCommit": "abc1", "envMode": "full", "score": 65, "criticalCount": 2 },
  { "run": 2, "timestamp": "...", "gitCommit": "abc2", "envMode": "full", "score": 79, "criticalCount": 0 },
  { "run": 3, "timestamp": "...", "gitCommit": "abc2", "envMode": "full", "score": 82, "criticalCount": 0 }
]
```

**Kun disse felter gemmes pr. run:** `run`, `timestamp`, `gitCommit`, `envMode`, `score`, `criticalCount`, `majorCount`, `minorCount`. Ingen full rule-details — det er `governance-report.json`'s ansvar.

**Brug:**
- Freeze mode (§21) læser de 3 seneste entries for aktiverings-check
- Stagnations-analyse: 5 runs med `scoreDelta ≤ 2` → hård stagnation, eskalér til bruger
- Historik slettes ALDRIG automatisk (purge er manuel operation)

**Grænse:** Fil > 500 entries → warn, men stop ikke. Historikfil er altid append-only.

---

## 27. RULE COVERAGE

**Problem:** Systemet ved ikke om alle 13 assertions faktisk eksekveres. Hvis `RunQualityGatesAsync` springer én over stille, forsvinder den fra scoring uden advarsel.

**Nyt felt `coverage` i `governance-report.json`:**

```json
"coverage": {
  "expectedRules": 13,
  "executedRules": 11,
  "missingRules": ["layout.consistency", "layout.interactive_elements_visible"],
  "coveragePercent": 85
}
```

**Kilde for `expectedRules`:**
`ruleKey`-mapping tabellen i §6 er autoritativ liste. `UiGovernanceRunner` loader denne som konstant ved opstart — ikke aflæst fra `RunQualityGatesAsync`.

**Konsekvens ved manglende regler:**
- `coveragePercent < 100` → score annoteres med `"scoreBasis": "partial"` i rapporten
- `"scoreBasis": "partial"` scores MÅ IKKE sammenlignes med `"scoreBasis": "full"` i delta
- Loop: fortsætter normalt, men annoterer delta med `"incompleteCoverage": true`
- Freeze mode (§21): aktiveres KUN hvis `coveragePercent = 100`

**Løsning for Unknown nr. 1 (§3):** Dette er det direkte svar på det uafklarede spørgsmål om `RunQualityGatesAsync` scope — systemet opdager selv manglende rules i stedet for at antage 100% coverage.

---

## 28. ENV MODE

**Styrer scope af hvad governance kører.**

**ENV var:** `GREENAI_GOVERNANCE_MODE` (default: `full`)

| Mode | Sider | Browsers | Screenshots | Verbose log |
|---|---|---|---|---|
| `fast` | `priority=high` sider kun | Chromium only | Nej | Nej |
| `full` | Alle sider i `ui_targets.json` | Chromium + Firefox + WebKit (jf. §14) | Ja | Nej |
| `debug` | Alle sider | Chromium only | Ja + focus shots | Ja |

**Regler:**
- `fast`-runs MÅ IKKE bruges til freeze-aktivering (§21 kræver `full`)
- `fast`-runs MÅ sammenlignes med hinanden i delta IF `uiTargetsHash` matcher
- `debug` producerer aldrig `governance-freeze.json` og afbryder aldrig CI
- `runSignature.envMode` (§24) sikrer at delta-sammenligning kun sker within samme mode

**Brug i `run-ui-autofix.ps1`:**
```
$env:GREENAI_GOVERNANCE_MODE = "fast"  # hurtig-iteration under aktiv fix
$env:GREENAI_GOVERNANCE_MODE = "full"  # final verify inden merge
```

---

## 29. FAILURE CLASSIFICATION

**Problem:** `severity` siger vigtighed. `ruleKey` siger domæne. Men ingen felt siger hvad slags fejl det er — hvilket gør analytics og grouping umuligt.

**Nyt felt `failureType` på `GovernanceRuleResult`:**

| `failureType` | Triggers | Copilot-relevans |
|---|---|---|
| `layout-overflow` | `layout.no_horizontal_overflow`, `layout.consistency` | CSS width/overflow fix |
| `missing-token` | `tokens.*` | CSS custom property mangler eller er hardcoded |
| `clipping` | `layout.topbar_not_clipping` | paddingTop / z-index fix |
| `z-index-collision` | `z-index.no_overlapping_clickable` | stacking context fix |
| `spacing-violation` | `layout.reasonable_spacing`, `layout.spacing_scale` | token-baseret padding/margin fix |
| `typography-violation` | `typography.*` | font-size / line-height fix |
| `component-error` | `component.no_visible_errors` | Blazor render / null-ref fix |
| `navigation-broken` | `component.navigation_usable` | nav-toggle / link fix |
| `browser-inconsistency` | `meta.cross_browser_inconsistency` | browser-specific CSS fix |

**Brug:**
- `copilot-fix-input.json` inkluderer `failureType` som supplement til `fixStrategy`
- `governance-history.json` kan group by `failureType` for trend-analyse
- `governance-escalations.json` bruger `failureType` som tag for menneskelig triage

**Sættes af:** `UiGovernanceRunner` via `ruleKey`-opslag (statisk mapping, ingen runtime logik).

---

## 30. OPERATIONS LAYER — SAMLET OVERBLIK

De 7 foregående sektioner udgør tilsammen systemets **drift-lag** — adskilt fra execution-laget (§10–15) og arkitektur-laget (§6–9).

**Fil-ansvar:**

| Fil | Lag | Ejer | Aldrig slettes |
|---|---|---|---|
| `governance-report.json` | Execution | Test-runner | Nej — overskrives pr. run |
| `governance-delta.json` | Execution | PS-script | Nej — overskrives pr. iteration |
| `governance-history.json` | Drift | PS-script | **JA** — append-only |
| `governance-freeze.json` | Governance | PS-script | Nej — auto-slet ved unfreeze |
| `governance-escalations.json` | Drift | PS-script + Runner | Nej — akkumuleres inden for session |
| `copilot-fix-input.json` | Execution | PS-script | Nej — overskrives pr. iteration |
| `rule-changelog.json` | Drift | Manuel | **JA** — permanent record |
| `ui_targets.json` | Config | Manuel | **JA** — source of truth |
| `component-ownership.json` | Config | Manuel | **JA** — source of truth |

**Pillar-model (komplet):**

```
CONFIG           EXECUTION          DRIFT
ui_targets       governance-report  governance-history
component-       governance-delta   rule-changelog
 ownership       copilot-fix-input  governance-
rule-changelog                       escalations
                GOVERNANCE
                governance-freeze
```

Ingen fil i CONFIG eller DRIFT overskrives automatisk. Kun EXECUTION-filer er volatile. Governance-filen (`governance-freeze.json`) er auto-managed men har single-toggle semantik.

---

## 31. FIX CONTRACT

**Problem:** `copilot-fix-input.json` siger HVAD og HVOR der skal fixes — men ikke hvad Copilot MÅ gøre. Uden kontrakt kan Copilot lave 3 semantisk ækvivalente fixes på 3 måder, introducere inline styles, eller oprette nye filer. Det skaber drift.

**Nyt felt `fixContract` i `copilot-fix-input.json`:**

```json
"fixContract": {
  "allowedActions": [
    "modify-existing-css-rule",
    "add-css-rule-to-existing-file"
  ],
  "forbiddenActions": [
    "create-new-file",
    "modify-razor-unless-component-owned",
    "add-inline-style",
    "modify-js"
  ],
  "tokenOnly": true,
  "maxLinesChanged": 5
}
```

**Felter:**

| Felt | Semantik |
|---|---|
| `allowedActions` | Udtømmende liste — enhver handling IKKE på listen er forbudt |
| `forbiddenActions` | Eksplicitte no-ops — Copilot stopper med forklaring |
| `tokenOnly` | `true` = CSS-ændringer MÅ KUN bruge `var(--...)` tokens — ingen hex, ingen numeriske px-værdier uden token |
| `maxLinesChanged` | Copilot stopper og eskalerer hvis fix kræver flere linjer |

**Copilot-regel:** Hvis fix ikke kan laves indenfor contract → `fixStrategy = report-only` + skriv til `governance-escalations.json` med `reason: "contract-violation-required"`. Copilot gætter ALDRIG en løsning outside contract.

**Contract sættes af:** `run-ui-autofix.ps1` baseret på `allowedCategory` + `failureType` (§29). CSS-kategorier giver altid `tokenOnly: true`. `component`-kategori tillader `modify-razor-unless-component-owned` men kræver at `blazor_component` er sat i `component-ownership.json`.

**Stabiliserings-effekt:** Samme `ruleKey` + samme `failureType` → altid samme `fixContract` → Copilot output er deterministisk pr. iteration over tid.

---

## 32. ROOT CAUSE ID

**Problem:** Samme underliggende CSS-fejl kan trippe 3 regler (`layout.no_horizontal_overflow`, `layout.consistency`, `layout.topbar_not_clipping`) som 3 separate violations. Systemet fixer den tre gange eller aldrig lukker den.

**Ny felt på `GovernanceRuleResult`:**

| Felt | Type | Hvem sætter det |
|---|---|---|
| `rootCauseId` | string \| null | `UiGovernanceRunner` via heuristik + selector-overlap |
| `rootCauseHint` | string \| null | Statisk mapping fra `ruleKey` + `failureType` |

**`rootCauseId`-format:** `{area}.{problem}.{specifikt-element}` — f.eks. `layout.overflow.grid-width`, `tokens.missing.color-primary`, `z-index.stacking.nav-overlay`.

**Heuristik for sammenfald:** Hvis 2+ rules fejler med overlappende selector → tildeles samme `rootCauseId`. Sættes i runner efter alle rules er eksekveret (post-pass).

**Effekt på loop:**

| Situation | Handling |
|---|---|
| 2 rules deler `rootCauseId` | én fix i `copilot-fix-input.json` — ikke to |
| Fix løser begge rules | begge markeres `fixed` i delta |
| Fix løser kun én | delta viser `rootCauseId` som `"partiallyFixed"` |

**Tracking i historik:** `governance-history.json` inkluderer `uniqueRootCauses` pr. run. Reelt fremskrid måles som fald i `uniqueRootCauses`, ikke i `ruleFailCount`.

**Fallback:** `rootCauseId = null` → behandles som unik — ingen deduplication. Copilot ser stadig alle violations men får ingen hint om overlap.

---

## 33. PAGE DISCOVERY OG COVERAGE GUARANTEE

**Problem:** `ui_targets.json` er en manuel liste. Den kan mangle sider. Et perfekt score på de testede sider giver en falsk tryghed hvis 5 sider aldrig er tilføjet.

**Route scanner — to strategier:**

### Strategi A — Link-crawler (Playwright)
Navigér til app root → evaluer alle `<a href>`-attributter der starter med `/` → dedup → sammenlign mod `ui_targets.json`.

### Strategi B — Blazor router reflection
Scan `src/GreenAi.Api/**/*.razor` for `@page "/..."` direktiver (statisk grep) → producér kanonisk route-liste uden browser.

**Strategi B foretrækkes:** Den er 10× hurtigere, kræver ikke app kørende, og er deterministisk. Strategi A som fallback til dynamisk-genererede routes.

**Output: `governance-coverage-report.json`:**

```json
{
  "totalRoutes": 18,
  "testedRoutes": 13,
  "coveragePercent": 72,
  "missingRoutes": [
    "/admin/logs",
    "/settings/advanced"
  ],
  "dynamicRoutes": ["/profile/{id}"]
}
```

**`dynamicRoutes`:** Routes med parametre (`{id}`, `{customerId}`) kan ikke automatisk testes — listes separat, aldrig som `missing`.

**Fail-betingelse:**

| `coveragePercent` | Handling |
|---|---|
| 100% | OK |
| 80–99% | WARN i rapport — loop fortsætter |
| < 80% | FAIL — exit 1 med besked: `"Route coverage below threshold"` |

**Opdatering af `ui_targets.json`:** Scanner foreslår manglende routes som `"suggestedAdditions"` i `governance-coverage-report.json`. Tilføjelse er altid manuel — systemet foreslår, bruger beslutter.

**Freeze mode (§21):** Aktiveres KUN hvis `coveragePercent = 100`. Partial coverage = ingen freeze uanset score.

---

## 34. VISUAL CONSISTENCY SCORE

**Problem:** Alle nuværende rules måler korrekthed — ikke konsistens. UI kan have 9 spacing-varianter, 4 fontfarver og 6 borderradii der alle er "korrekte" tokens men visuelt inkonsistente.

**Nye konsistens-metrics som supplement til `dimensions` (§7):**

```json
"visualConsistency": {
  "spacingVariants": 3,
  "fontSizeVariants": 2,
  "colorVariants": 1,
  "borderRadiusVariants": 2,
  "consistencyScore": 0.84
}
```

**Beregning:**
- `spacingVariants` = antal unikke computed `padding`/`margin`-værdier på `.mud-card`, `.hub-panel`, `.ga-page-content`
- `fontSizeVariants` = antal unikke computed `font-size`-værdier på `p`, `.mud-typography-body1`
- `colorVariants` = antal unikke computed `color`-værdier på body-tekst (ekskl. headings + links)
- `consistencyScore` = `1 - (totalVariants - expectedVariants) / totalElements` (0–1)

**Threshold:** `consistencyScore < 0.75` → ny ruleKey: `meta.visual_consistency` med severity `minor`.

**Brug:** Supplement til eksisterende score — indgår i `dimensions.meta` men vægtes lavt (minor = 5 points). Primært til trend-analyse i historik.

---

## 35. PERFORMANCE METRICS

**Problem:** UI kan bestå alle governance rules men have 3000ms render eller CLS = 0.35. Det er en brugeroplevelse-fejl der ikke fanges af DOM-assertions.

**Nye felter i `governance-report.json`:**

```json
"performance": {
  "firstRenderMs": 420,
  "layoutShiftScore": 0.12,
  "domContentLoadedMs": 310,
  "interactiveMs": 680
}
```

**Indsamling:** Playwright Web Vitals via `page.evaluate()` post-load. Kræver ingen ekstern library.

**Thresholds (ikke-blokerende i v1):**

| Metric | WARN | FAIL (v2) |
|---|---|---|
| `firstRenderMs` | > 1000ms | > 3000ms |
| `layoutShiftScore` | > 0.1 | > 0.25 |

**Scope:** Performance-metrics er observability-data — de fejler ingen rules og blokerer ikke loop i Phase 1. De gemmes i `governance-history.json` til trend-analyse. Threshold-baseret failing introduceres i Phase 2 som egne `meta.performance.*` rules.

---

## 36. ANTI-FLAKINESS LAYER

**Problem:** Playwright + Blazor Server + SignalR = race conditions. En test der fejler én gang og passer næste er ikke en governance-fejl — det er en stabilitets-fejl. Disse er de farligste fejl fordi de skaber noise i governance-data.

**To mekanismer:**

### Mekanisme A — Assertion-level retry

```
try { await AssertX() }
catch (Exception ex) {
  await Task.Delay(150);  // settle
  try { await AssertX() }  // retry 1
  catch { result.passed = false; result.flakeRetried = true; }
}
```

Maks 1 retry per assertion. `flakeRetried = true` logges på result — aldrig skjult.

### Mekanisme B — Smart stabilization (ingen fixed delay)

I stedet for `Task.Delay(500)`: vent på specifik DOM-betingelse.

| Betingelse | Wait-strategi |
|---|---|
| SignalR ikke connected | `WaitForSelectorAsync("[data-signalr='connected']", timeout: 5s)` |
| Data ikke loaded | `WaitForSelectorAsync("[data-loaded='true']", timeout: 5s)` |
| Skeleton synlig | `WaitForSelectorAsync(".mud-skeleton", State = Hidden, timeout: 3s)` |

Fallback hvis ingen signal: `await page.WaitForLoadStateAsync(LoadState.NetworkIdle)` (allerede i `VisualTestBase.cs`).

**Flakiness-tracking i rapport:**

```json
"stability": {
  "flakyAssertions": ["layout.consistency"],
  "retryCount": 1,
  "stabilityScore": 0.92
}
```

`flakyAssertions[]` vises i rapport men disqualificerer ikke fix — Copilot ser dem med `confidence = low` automatic override hvis `flakeRetried = true`.

**Freeze mode (§21):** Aktiveres KUN hvis `stability.flakyAssertions` er tom liste. Flaky tests = ingen freeze.

---

*Sidst opdateret: 2026-04-07 (v10 — fix contract, rootCauseId, page discovery + coverage, visual consistency, performance metrics, anti-flakiness)*

---

## 10. SYSTEM BINDING — FLOW

**Hvem kalder hvad:**

`UiGovernanceTests.cs` er den eneste entry point — en xUnit test med `Category=UIGovernance`. Den kalder `UiGovernanceRunner`, som kalder `GovernanceScorer`, som skriver `governance-report.json`. Derefter læser `run-ui-autofix.ps1` JSON'en.

**Test lifecycle position:**

```
SharedAuth.PrimaryAsync()
→ page.GotoAsync(url)
→ ForEachDeviceAsync
    → UiGovernanceRunner (pr. device)
        → GovernanceScorer
→ Serialize governance-report.json
→ xUnit Assert (kun på criticals + iteration)
→ Test returnerer (pass eller fail)
```

**Result flow:**

| Destination | Mekanisme | Timing |
|---|---|---|
| Scoring | In-process direkte kald | Synkront, inden test returnerer |
| Autofix-loop | Fil-poll: `governance-report.json` | Asynkront, efter test exit |
| Copilot | `copilot-fix-input.json` genereret af PS-script | Næste iteration |

**Kritisk detalje:** Copilot ser aldrig `governance-report.json` direkte. PS-scriptet transformerer det til `copilot-fix-input.json` med `allowedCategory` og prioriteret fix-liste. Denne buffer er afgørende — den filtrerer information til hvad Copilot _må_ handle på.

---

## 11. RULE ENGINE — HVORFOR DET IKKE ER EN RIGTIG RULE ENGINE ENDNU

**Nuværende situation:** Assertions er imperativ kode der **både definerer og eksekverer** rules. `ruleKey`-mapping er hardcodet i `UiGovernanceRunner`. Tilføjelse af ny regel = kode-ændring.

**Hvad en rigtig rule engine kræver:**

En deklarativ regel er data, ikke kode. Formel definition:

```
regel = { ruleKey, assertionMethod, severity, confidence, suggestedArea, ruleVersion, enabled }
```

Execution engine registrerer assertion-metoder under `assertionMethod`-navn og dispatcher fra data. Regler kan da:
- Aktiveres/deaktiveres pr. environment uden deploy
- Genbruges på tværs af sider med forskellig severity
- Versiones uafhængigt af kode

**Vejen fra assertions → declarative:**

1. `rules.json` indeholder alle rules som data
2. `UiGovernanceRunner` indlæser `rules.json` og mapper `assertionMethod` → C#-delegate via dictionary
3. Assertions forbliver uændrede — de er nu blot named functions i et registry
4. Ny regel = ny linje i `rules.json` + eventuelt ny assertion-metode

**Hvad vi IKKE har endnu:** Registry, JSON-loader og dispatch-lag. Alt er stadig hardcoded i runner-klassen. Det er Phase 2 — ikke Phase 1 (minimal slice).

---

## 12. REGRESSION DETECTION — PRÆCIST FLOW

**Hvad der sammenlignes:**

`scoreDelta` = `score(iteration N)` − `score(iteration N-1)`. Beregnes i PS-scriptet efter hvert test-run.

**Stop-betingelser i prioriteret rækkefølge:**

```
1. score ≥ 80 AND criticalCount = 0              → exit 0 (SUCCESS)
2. scoreDelta < 0                                → exit 1 (REGRESSION)
3. scoreDelta = 0 for 2 på hinanden følgende     → exit 1 (STUCK)
4. iterations > MaxIterations                    → exit 1 (TIMEOUT)
5. criticalCount > 0 AND iteration >= 2          → exit 1 (CRITICAL UNFIXED)
```

**Hvad delta-filen gemmer pr. iteration:**

```
{ iteration, score, scoreDelta, reason, fixed[], newFailures[], unchanged[] }
```

`newFailures` er det vigtigste felt: viser præcist hvilke `ruleKey`-værdier Copilot brød, som var passed i forrige iteration.

**Checkpoint-mekanisme:** Systemet gemmer kun `governance-report.json` fra seneste iteration. Hvis Copilot fixer R1 i iteration 2 men bryder R4 → `newFailures: ["z-index.no_overlapping_clickable"]` i delta → STOP. Copilot ser nøjagtigt hvad den introducerede.

---

## 13. STATE COVERAGE — PLAYWRIGHT DESIGN

**Kerneindsigt:** De 4 states kræver **forskellig test-setup**, ikke forskellig screenshot-timing.

| State | Setup-krav | Playwright-mekanisme |
|---|---|---|
| `success` | Ingen — normal flow | `await networkidle` → screenshot |
| `loading` | Ingen — timing vindue | Screenshot umiddelbart efter `GotoAsync`, inden `networkidle` |
| `empty` | Route-interception | `page.RouteAsync` → returnér tom array |
| `error` | Route-interception | `page.RouteAsync` → returnér HTTP 500 |

**Loading-state timing:** `GotoAsync` + screenshot i ét kald er ikke stabilt — SignalR render-speed varierer. Løsning: screenshot umiddelbart efter navigation, derefter vent på specifik skeleton-selector (`[data-testid*="skeleton"]` eller `.mud-skeleton`) med 200ms timeout. Hvis skeleton ikke ses → `loading`-state er for hurtig til at indfange.

**Empty + Error via route-interception:** `page.RouteAsync("**/api/broadcasting/**", ...)` kan intercepte alle API-kald og returnere mock-respons. Det kræver ingen ændring af app-koden — Playwright-native, ingen test-tenant-setup.

**Prioritering:** `success` og `error` i minimal slice. `loading` er fragil og nice-to-have. `empty` kræver route-interception ligesom `error` — slice 2.

---

## 14. CROSS-BROWSER PLAN

**Baseline:** Playwright har native Chromium, Firefox og WebKit. Assertions er DOM-baserede og JS-evaluated — ikke pixel-baserede. De fleste regler er browser-uafhængige.

**Hvad er browser-sensitivt:**

| Regel | Browser-sensitiv? | Begrundelse |
|---|---|---|
| `layout.no_horizontal_overflow` | JA | Subpixel-rounding varierer |
| `tokens.primary_color` | NEJ | Computed CSS er spec-deterministic |
| `typography.font_scale` | DELVIST | Font-rendering threshold kan variere |
| `z-index.no_overlapping_clickable` | JA | z-index stacking context varierer |
| `component.no_visible_errors` | NEJ | DOM-elementers tilstedeværelse er browser-uafhængig |

**Differentieret browser-matrix:**

```
Chromium:  alle 4 devices × alle 13 regler        (full run)
Firefox:   Desktop only × browser-sensitive rules  (overflow, z-index, font)
WebKit:    Desktop only × browser-sensitive rules  (overflow, z-index, font)
```

Tidsforhold: 1× (Chromium) + 0.2× (Firefox) + 0.2× (WebKit) ≈ 1.4× total vs. 3× for fuld matrix.

**Sammenligning via `browser-delta.json`:**

```json
{ "ruleKey": "layout.no_horizontal_overflow", "chromium": "pass", "firefox": "fail", "webkit": "pass" }
```

**Copilot-regel:** Violation kun på Firefox/WebKit → `"browserScope": "firefox-only"` i `copilot-fix-input.json` → Copilot tilføjer browser-specifikt CSS (`@supports` eller targeted override), ikke global fix.

---

## 15. END-TO-END SYSTEM DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                        PLAYWRIGHT                               │
│  GotoAsync(url) → ForEachDevice(4) × Browser(1–3)              │
└───────────────────────────┬─────────────────────────────────────┘
                            │ DOM loaded
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     DOM EVALUATION                              │
│  JS evaluate inside browser:                                    │
│  scrollWidth, computedStyle, elementFromPoint, axe-core         │
└───────────────────────────┬─────────────────────────────────────┘
                            │ raw pixel values + computed CSS
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│               RULE EXECUTION (UiGovernanceRunner)               │
│  try/catch per assertion → GovernanceRuleResult                 │
│  ruleKey + passed + message + selector + selectorType           │
│                   + confidence + suggestedArea                  │
└───────────────────────────┬─────────────────────────────────────┘
                            │ List<GovernanceRuleResult>
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                SCORING (GovernanceScorer)                       │
│  weighted sum (critical=50, major=15, minor=5)                  │
│  → score (0–100) + dimensions{layout, tokens, typography...}    │
│  → governance-report.json                                       │
└───────────────────────────┬─────────────────────────────────────┘
                            │ JSON file on disk
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│              LOOP CONTROL (run-ui-autofix.ps1)                  │
│                                                                 │
│  READ governance-report.json                                    │
│  COMPUTE scoreDelta vs. previous iteration                      │
│                                                                 │
│  score ≥ 80 AND criticals = 0?  ──── YES ──→ exit 0 ✅         │
│  scoreDelta < 0?                ──── YES ──→ exit 1 ❌ REGRESS  │
│  stuck (Δ=0 ×2)?                ──── YES ──→ exit 1 ❌ STUCK    │
│  criticals AND iter ≥ 2?        ──── YES ──→ exit 1 ❌ CRITICAL │
│         │                                                       │
│         NO → transform → copilot-fix-input.json                │
│              { allowedCategory, selectorType, fixes[] }         │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      COPILOT FIX                                │
│  READ copilot-fix-input.json                                    │
│  selectorType == data-testid?  → AUTO-FIX                       │
│  selectorType == fallback?     → REPORT ONLY                    │
│  suggestedArea == unknown?     → STOP, ESCALATE                 │
│  ONE allowedCategory per iteration                              │
└───────────────────────────┬─────────────────────────────────────┘
                            │ code change committed
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                        VERIFY                                   │
│  governance-delta.json updated                                  │
│  newFailures[] checked BEFORE next iteration starts             │
│  newFailures > 0? → STOP (Copilot introduced regression)        │
│  N++ → return to PLAYWRIGHT                                     │
└─────────────────────────────────────────────────────────────────┘
```

**Kritisk observation:** Der er ingen direkte forbindelse mellem Copilot og Playwright. Copilot skriver filer. PS-scriptet starter næste test-run. Systemet kan køre uden Copilot til manuel verify. Det er den egentlige separation of concerns.
