# Localization — SSOT

> Authoritative documentation for the label/translation system.

**Last Updated:** 2026-04-02

---

## Quick Navigation

| File | Topic |
|------|-------|
| [label-creation-guide.md](guides/label-creation-guide.md) | How to add new labels (shared.* vs feature.*) |
| [shared-labels-reference.md](reference/shared-labels-reference.md) | Catalog of all `shared.*` keys with DA/EN values |
| [loc-helper.md](guides/loc-helper.md) | `Loc` static helper for Blazor components |

---

## Label Workflow (AI: FØLG DETTE)

```
STEP 1: Tjek eksisterende labels (DRY!)
   pwsh -File scripts/localization/Get-Labels.ps1 -Filter "shared.*"

STEP 2: Opret MANGLENDE labels i LIVE (itgain.dk) FØRST — inden kodeændringer
   $labels = @(
       @{ ResourceName="shared.MyLabel"; ResourceValue="Dansk";  LanguageId=1 }
       @{ ResourceName="shared.MyLabel"; ResourceValue="English"; LanguageId=2 }
   )
   pwsh -File scripts/localization/Add-Labels.ps1 -Labels $labels

STEP 3: Sync til lokal dev DB (så UI ikke viser nøgler under debugging)
   pwsh -File scripts/localization/Sync-Labels.ps1

STEP 4: Opdater kode (Blazor @Loc.Get("shared.MyLabel"))
```

**KRITISK:**
- ⛔ ALDRIG SQL direkte mod Labels-tabellen
- ⛔ ALDRIG localhost — altid `https://itgain.dk`
- ⛔ ALDRIG opret feature.SaveButton hvis shared.SaveButton eksisterer
- ✅ Credentials: `appsettings.Production.json` → `LabelManagementApi` (gitignored)
- ✅ LanguageId: 1=Dansk, 2=English

---

## Architecture

```
Languages table  ← LanguageId (1=DA, 2=SV, 3=EN, 4=FI, 5=NO, 6=DE)
Labels table     ← ResourceName (key), ResourceValue (text), LanguageId

ILocalizationRepository  →  SQL against Labels table
ILocalizationService     →  fail-open (returns key if missing)
Loc static helper        →  @Loc.Get("key") in Blazor
```

---

## Key Rules

```
✅ Keys: dot-notation  shared.SaveButton / feature.customer.NameLabel
✅ ALWAYS create BOTH DA (langId=1) AND EN (langId=3)
✅ shared.* for generic UI concepts (Save, Cancel, Delete, Status...)
✅ feature.* ONLY for truly feature-specific content
✅ ILocalizationService is fail-open (returns key if label missing)
❌ NEVER hardcode strings in Blazor — use @Loc.Get("key")
❌ NEVER create feature.SaveButton when shared.SaveButton exists
```

---

## Label Naming Convention

| Suffix | Use for | Example |
|--------|---------|---------|
| `Button` | Buttons | `shared.SaveButton` |
| `Label` | Field labels, status text | `shared.StatusLabel` |
| `Column` | Table column headers | `shared.ColumnName` |
| `Placeholder` | Input placeholders | `shared.SearchPlaceholder` |
| `Format` | Template strings with `{0}` | `shared.DeleteConfirmFormat` |

---

## Existing `shared.*` Labels (DA / EN)

| Key | DA | EN |
|-----|----|----|
| `shared.SaveButton` | Gem | Save |
| `shared.CancelButton` | Annuller | Cancel |
| `shared.DeleteButton` | Slet | Delete |
| `shared.EditButton` | Rediger | Edit |
| `shared.CloseButton` | Luk | Close |
| `shared.ClearButton` | Ryd | Clear |
| `shared.ExportButton` | Eksport | Export |
| `shared.RefreshButton` | Opdater | Refresh |
| `shared.CreateEntityButton` | Opret ny {0} | Create new {0} |
| `shared.SearchPlaceholder` | Søg... | Search... |
| `shared.StatusLabel` | Status | Status |
| `shared.ColumnName` | Navn | Name |
| `shared.ColumnStatus` | Status | Status |
| `shared.SaveSuccess` | Gemt! | Saved! |
| `shared.DeleteConfirmFormat` | Er du sikker på at slette {0}? | Are you sure you want to delete {0}? |

See [reference/shared-labels-reference.md](reference/shared-labels-reference.md) for complete catalog.

---

**Last Updated:** 2026-04-02
