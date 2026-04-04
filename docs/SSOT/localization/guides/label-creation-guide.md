# Label Creation Guide

> **Canonical:** SSOT for creating new localization labels in green-ai.
> **Enforcement:** No AI may hardcode strings — see also [loc-helper.md](loc-helper.md).

```yaml
id: label_creation_guide
type: guide
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/localization/guides/label-creation-guide.md
red_threads: []
applies_to: ["**/*.razor"]
enforcement: Validate-GreenAiCompliance.ps1 (LOC-001 — planned)
```

---

## ⚠️ MANDATORY PRE-CREATION CHECKLIST

**Kør dette FØR du opretter ETHVERT label:**

```
[ ] 1. Er det et generisk UI-koncept? (se FORBIDDEN-tabel nedenfor)
       → JA: brug shared.* (ALDRIG feature-specifik!)
       → NEJ: gå til trin 2

[ ] 2. Søg i shared-labels-reference.md — findes det allerede?
       → JA: brug det eksisterende key
       → NEJ: gå til trin 3

[ ] 3. Bruges det på 2+ features?
       → JA: opret som shared.*
       → NEJ: gå til trin 4

[ ] 4. Er det forretningslogik specifik for ÉN feature?
       → JA: opret som feature.*
       → NEJ: default til shared.*

[ ] 5. Valider navnekonvention (se tabel nedenfor)

[ ] 6. Opret ALTID BEGGE: DA (LanguageId=1) OG EN (LanguageId=2)
```

---

## 🔴 FORBIDDEN — Opret ALDRIG som feature.*

**Disse er generiske UI-koncepter — ALTID shared.\*:**

### Buttons

| Korrekt shared.* | FORBUDT feature.* eksempel |
|---|---|
| `shared.SaveButton` | ❌ feature.settings.SaveButton |
| `shared.CancelButton` | ❌ feature.user.CancelButton |
| `shared.DeleteButton` | ❌ feature.profile.DeleteButton |
| `shared.EditButton` | ❌ feature.user.EditButton |
| `shared.CloseButton` | ❌ feature.drawer.CloseButton |
| `shared.ClearButton` | ❌ feature.filter.ClearButton |
| `shared.ClearFiltersButton` | ❌ feature.list.ClearFiltersButton |
| `shared.ExportButton` | ❌ feature.user.ExportButton |
| `shared.RefreshButton` | ❌ feature.list.RefreshButton |
| `shared.CreateEntityButton` (format) | ❌ feature.*.CreateButton |

**🔴 REGEL:** Brug altid `string.Format(Loc.Get("shared.CreateEntityButton"), Loc.Get("shared.{Entity}").ToLower())` frem for feature-specifikke create-buttons.
- `.ToLower()` på entity-navnet giver korrekt "Opret ny bruger" (ikke "Opret ny Bruger")

### Data-felter

| Korrekt shared.* | FORBUDT feature.* eksempel |
|---|---|
| `shared.StatusLabel` / `shared.ColumnStatus` | ❌ feature.user.StatusLabel |
| `shared.EmailLabel` / `shared.ColumnEmail` | ❌ feature.adminUsers.EmailColumn |
| `shared.NameLabel` / `shared.ColumnName` | ❌ feature.user.NameColumn |
| `shared.SearchPlaceholder` | ❌ feature.list.SearchInput |
| `shared.Note` / `shared.NotesLabel` | ❌ feature.customer.Notes |

### Messages

| Korrekt shared.* | FORBUDT feature.* eksempel |
|---|---|
| `shared.SaveSuccess` | ❌ feature.settings.SavedMessage (brug kun til feature-specifik tekst) |
| `shared.SaveErrorFormat` | ❌ feature.form.SaveError |
| `shared.DeleteErrorFormat` | ❌ feature.list.DeleteError |
| `shared.DeleteConfirmFormat` | ❌ feature.user.DeleteConfirm |

---

## Decision Tree: shared.* vs feature.*

```
Er teksten et generisk UI-koncept (Save, Cancel, Delete, Status...)?
  JA  → brug shared.* key (tjek shared-labels-reference.md først)
  NEJ → bruges det på 2+ features?
          JA  → feature.{domain}.{purpose}   f.eks. feature.profile.NoRecords
          NEJ → feature.{domain}.{purpose}   (accepter som feature-specifik)
```

---

## Key Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Shared button | `shared.{Action}Button` | `shared.SaveButton` |
| Shared column header | `shared.Column{Name}` | `shared.ColumnName` |
| Shared status/label | `shared.{Name}Label` | `shared.StatusLabel` |
| Shared message | `shared.{Event}Message` | `shared.DeletedMessage` |
| Shared confirm format | `shared.{Action}ConfirmFormat` | `shared.DeleteConfirmFormat` |
| Feature label | `feature.{domain}.{Purpose}` | `feature.profile.NoRecords` |

**Key rules:**
- `PascalCase` after the last `.`
- Always create DA (LanguageId=1) AND EN (LanguageId=3)
- Check `shared-labels-reference.md` before creating new shared.* keys

---

## Step-by-Step: Adding a New Label

### Step 1 — Check if label already exists

```
See: docs/SSOT/localization/reference/shared-labels-reference.md
```

If existing — use it. Do NOT create a duplicate.

### Step 2 — Create a DB migration

```sql
-- V017_NewFeatureLabels.sql (or append to existing pending migration)
INSERT INTO [dbo].[Labels] ([ResourceName], [ResourceValue], [LanguageId])
SELECT v.[ResourceName], v.[ResourceValue], v.[LanguageId]
FROM (VALUES
    ('feature.profile.CreateButton',   'Opret profil',  1),
    ('feature.profile.CreateButton',   'Create profile',3)
) AS v ([ResourceName], [ResourceValue], [LanguageId])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Labels] l
    WHERE l.[ResourceName] = v.[ResourceName] AND l.[LanguageId] = v.[LanguageId]
);
```

Migration naming: `V{next}_SeedLabels_{domain}.sql`

### Step 3 — Use in Razor

```razor
@inject ILocalizationContext Loc

<MudButton>@Loc.Get("feature.profile.CreateButton")</MudButton>
```

### Step 4 — Update shared-labels-reference.md

If you added `shared.*` labels, add them to the reference catalog.

---

## ILocalizationContext Behavior

- **Fail-open:** If a key is not found in DB, `Get(key)` returns the key string itself
- **Language:** Loaded from `ICurrentUser.LanguageId` via `EnsureLoadedAsync`
- **Scope:** One instance per Blazor circuit (Scoped) — cached after first load

---

**Last Updated:** 2026-04-06
