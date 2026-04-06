# Color System — SSOT

**Token file:** `wwwroot/css/design-tokens.css`  
**Skin file:** `wwwroot/css/portal-skin.css`  
**MudTheme:** `Components/Layout/MainLayout.razor` (inline `<style id="greenai-palette-override">`)  
**E2E tests:** `tests/GreenAi.E2E/ColorSystem/ColorSystemTests.cs`

---

## Semantic Roles (LOCKED)

| Role         | Token                  | Hex       | Usage                                   |
|--------------|------------------------|-----------|-----------------------------------------|
| Primary      | `--color-primary`      | `#2563EB` | **Actions, navigation, primary buttons — ONLY** |
| Primary hover| `--color-primary-hover`| `#1D4ED8` | Hovered primary buttons / links         |
| Primary light| `--color-primary-light`| `#DBEAFE` | Tinted backgrounds for selected states  |
| Success      | `--color-success`      | `#16A34A` | Completed, confirmed, sent status       |
| Warning      | `--color-warning`      | `#D97706` | Pending, caution, non-critical alerts   |
| **Error**    | `--color-error`        | `#DC2626` | **Errors and failures ONLY**            |
| Info         | `--color-info`         | `#0284C7` | Informational, neutral status           |
| Bg main      | `--color-bg-main`      | `#F7F8FA` | Page/layout background                  |
| Bg surface   | `--color-bg-surface`   | `#FFFFFF` | Cards, papers, drawers                  |
| Text primary | `--color-text-primary` | `#1F2937` | Body text, headings                     |
| Text secondary| `--color-text-secondary`| `#6B7280`| Secondary labels, captions              |
| Text disabled| `--color-text-disabled`| `#9CA3AF` | Disabled inputs, labels                 |
| Border light | `--color-border-light` | `#E5E7EB` | Card borders, table lines               |
| Border medium| `--color-border-medium`| `#D1D5DB` | Stronger dividers                       |

---

## Rules

1. **Red is reserved for errors** — `Color.Error` / `--color-error` must only appear on:
   - Error alerts (`Severity.Error`)
   - Validation failures
   - Delete confirmations
   - Failed status badges
   - Never on navigation, primary actions, or decorative elements

2. **Blue is primary action / navigation** — `Color.Primary` / `--color-primary`:
   - All primary buttons
   - Active nav links
   - Action-focused chips / tabs
   - Never for status-only indicators

3. **No hardcoded hex in components** — use `var(--color-*)` tokens or MudBlazor's `Color.*` enum. Enforced by `CssTokenComplianceTests.cs`.

4. **New colors go through tokens** — never add raw hex to component CSS. Add token to `design-tokens.css` first.

---

## Utility Classes

Defined in `portal-skin.css`:

| Class                  | Purpose                                  |
|------------------------|------------------------------------------|
| `.ga-surface`          | White surface with light border          |
| `.ga-border`           | Light border only                        |
| `.ga-text-secondary`   | Secondary text color                     |
| `.ga-status-success`   | Green badge (success/completed)          |
| `.ga-status-warning`   | Orange badge (pending/caution)           |
| `.ga-status-error`     | Red badge (error/failed — use sparingly) |
| `.ga-status-info`      | Blue badge (informational)               |
| `.ga-btn-primary`      | Primary button for non-MudBlazor contexts|
| `.ga-focusable`        | Explicit focus ring for custom elements  |

---

## MudTheme Mapping

The inline `<style id="greenai-palette-override">` in `MainLayout.razor` and `WizardLayout.razor`
overrides `--mud-palette-*` variables to align MudBlazor components with the token SSOT:

```
--mud-palette-primary:    var(--color-primary)       #2563EB
--mud-palette-success:    var(--color-success)        #16A34A
--mud-palette-warning:    var(--color-warning)        #D97706
--mud-palette-error:      var(--color-error)          #DC2626
--mud-palette-info:       var(--color-info)           #0284C7
--mud-palette-background: var(--color-bg-main)        #F7F8FA
--mud-palette-surface:    var(--color-bg-surface)     #FFFFFF
--mud-palette-text-primary: var(--color-text-primary) #1F2937
```

---

## CSS Load Order

```
<link href="_content/MudBlazor/MudBlazor.min.css" />  ← MudBlazor base
<link href="css/design-tokens.css" />                  ← SSOT tokens (--color-*)
<link href="css/greenai-skin.css" />                   ← --ga-* tokens + MudBlazor component overrides
<link href="css/greenai-enterprise.css" />             ← Enterprise density / elevation layer
<link href="css/portal-skin.css" />                    ← Semantic skin using --color-* tokens
<style id="greenai-palette-override"> ... </style>     ← Inline MudTheme palette (MainLayout/WizardLayout)
```

---

## WCAG Notes

All token values target WCAG AA:
- `--color-text-primary` (#1F2937) on white: ~12:1
- `--color-primary` (#2563EB) on white: ~4.5:1 (AA pass for UI components)
- `--color-success` (#16A34A) on white: ~5.1:1
- `--color-error` (#DC2626) on white: ~5.3:1
- `--color-warning` (#D97706) on white: ~3.4:1 (border/icon use; pair with text if needed)

Focus ring: `3px solid var(--color-primary)` — meets WCAG 2.4.11 Focus Appearance.
