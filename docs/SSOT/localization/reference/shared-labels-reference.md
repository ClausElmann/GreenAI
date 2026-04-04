# Shared Labels Reference

> **Canonical:** Complete catalog of all `shared.*` localization keys.
> **Source of truth:** `V014_SeedSharedLabels.sql` + subsequent migrations.

```yaml
id: shared_labels_reference
type: reference
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/localization/reference/shared-labels-reference.md
red_threads: []
```

> **Before creating any new shared.* key:** check this file first. If it exists — use it.

---

## Buttons

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `shared.SaveButton` | Gem | Save | V014 |
| `shared.CancelButton` | Annuller | Cancel | V014 |
| `shared.DeleteButton` | Slet | Delete | V014 |
| `shared.EditButton` | Rediger | Edit | V014 |
| `shared.CloseButton` | Luk | Close | V014 |
| `shared.ClearButton` | Ryd | Clear | V014 |
| `shared.ClearFiltersButton` | Ryd filtre | Clear filters | API |
| `shared.ExportButton` | Eksport | Export | V014 |
| `shared.RefreshButton` | Opdater | Refresh | V014 |
| `shared.CreateEntityButton` | Opret ny {0} | Create new {0} | V014 |

## Column Headers

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `shared.ColumnName` | Navn | Name | V014 |
| `shared.ColumnStatus` | Status | Status | V014 |
| `shared.ColumnEmail` | E-mail | Email | V017 |
| `shared.ColumnId` | ID | ID | V017 |

## Labels & Status

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `shared.StatusLabel` | Status | Status | V014 |
| `shared.NameLabel` | Navn | Name | V017 |
| `shared.EmailLabel` | E-mail | Email | V017 |
| `shared.Active` | Aktiv | Active | V017 |
| `shared.Inactive` | Inaktiv | Inactive | V017 |
| `shared.Note` | Note | Note | API |
| `shared.NotesLabel` | Noter | Notes | API |
| `shared.User` | Bruger | User | API |

## Messages

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `shared.SaveSuccess` | Gemt! | Saved! | V014 |
| `shared.SaveErrorFormat` | Kunne ikke gemme: {0} | Could not save: {0} | API |
| `shared.DeleteConfirmFormat` | Er du sikker på at slette {0}? | Are you sure you want to delete {0}? | V014 |
| `shared.DeleteErrorFormat` | Kunne ikke slette: {0} | Could not delete: {0} | API |
| `shared.DeletedMessage` | {0} slettet. | {0} deleted. | V017 |
| `shared.Created` | Oprettet | Created | API |

## Input Placeholders

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `shared.SearchPlaceholder` | Søg... | Search... | V014 |

## Navigation

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `nav.Home` | Forside | Home | V017 |
| `nav.CustomerAdmin` | Kundestyre | Customer Admin | V017 |

---

## Feature-Specific Labels (non-shared)

### `feature.profile.*`

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `feature.profile.CreateButton` | Opret profil | Create profile | V017 |
| `feature.profile.DeleteTitle` | Slet profil | Delete profile | V017 |
| `feature.profile.NoRecords` | Ingen profiler. | No profiles. | V017 |
| `feature.profile.CreateNotImplemented` | Opret profil — ikke implementeret endnu. | Create profile — not yet implemented. | V017 |

### `feature.user.*`

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `feature.user.DeleteTitle` | Slet bruger | Delete user | V017 |
| `feature.user.NoRecords` | Ingen brugere. | No users. | V017 |
| `feature.user.CreateNotImplemented` | Opret bruger — ikke implementeret endnu. | Create user — not yet implemented. | V017 |

### `feature.settings.*`

| Key | DA | EN | Seeded |
|-----|----|----|--------|
| `feature.settings.SavedMessage` | Indstillinger gemt. | Settings saved. | V017 |

---

**Last Updated:** 2026-04-06
