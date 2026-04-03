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

## Decision Tree: shared.* vs feature.*

```
Is the string a generic UI concept (Save, Cancel, Delete, Status...)?
  YES → use shared.* key (check shared-labels-reference.md first)
  NO  → Is it shared across ALL features in the domain?
          YES → feature.{domain}.{purpose}   e.g. feature.profile.NoRecords
          NO  → feature.{domain}.{componentName}.{purpose}
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
