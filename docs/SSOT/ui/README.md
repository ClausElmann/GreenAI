# UI — SSOT

> Authoritative patterns for Blazor pages, components, and UI models in green-ai.

**Last Updated:** 2026-04-06

---

## Quick Navigation

| File | Topic |
|------|-------|
| [patterns/blazor-page-pattern.md](patterns/blazor-page-pattern.md) | Authenticated page lifecycle — OnAfterRenderAsync, PrincipalHolder, loading state |
| [patterns/blazor-component-pattern.md](patterns/blazor-component-pattern.md) | Reusable components — parameter contracts, event callbacks, no auth |
| [patterns/mudblazor-conventions.md](patterns/mudblazor-conventions.md) | MudBlazor usage rules — allowed components, forbidden patterns |
| [models/ui-navigation-schema.json](models/ui-navigation-schema.json) | Machine-readable navigation model — routes, auth requirements, breadcrumbs |
| [color-system.md](color-system.md) | Design token SSOT — --color-*, --font-*, --space-*, MudTheme mapping, WCAG notes |
| [component-system.md](component-system.md) | CSS utility classes — .ga-btn-*, .ga-card, .ga-table, .ga-col-numeric, .ga-chip-reset, .ga-icon-*, forms |

---

## CSS Architecture

### Load Order (canonical — must not change)

```
MudBlazor.min.css
app.css               ← --ga-* legacy tokens (aliased to --color-* since 2026-04-06)
design-tokens.css     ← SSOT: --color-*, --font-*, --space-*, --font-icon-*
greenai-skin.css      ← MudBlazor component overrides using --ga-* tokens
greenai-enterprise.css← Enterprise density: tables, cards, badges, active states
portal-skin.css       ← Semantic skin + .ga-* utility classes (loaded last, highest precedence)
<style id="greenai-palette-override"> ← MudTheme --mud-palette-* → var(--color-*)
```

### Token Layers

| File | Tokens | Rule |
|------|--------|------|
| `design-tokens.css` | `--color-*`, `--font-*`, `--space-*`, `--font-icon-*` | SSOT — never hardcode hex elsewhere |
| `app.css` | `--ga-*` | Legacy aliases bridged to `var(--color-*)` — do not add new --ga-* tokens |
| `portal-skin.css` | `.ga-*` classes | Component utility library — all new classes go here |

---

## Component System Rules

- ❌ `Style="text-align:right"` → `Class="ga-col-numeric"`
- ❌ `Style="margin:0"` on chips → `Class="ga-chip-reset"`
- ❌ `Style="font-size:Xrem"` on icons → `Class="ga-icon-xl / ga-icon-2xl (+ -dim / -faded variants)"`
- ❌ Plain `<button>` outside Layout/ without `ga-btn-*` class → governance test **fails**
- ❌ `MudTable` without `Dense="true"` → governance test **fails**
- ❌ `outline: none` without `box-shadow` or `border-color` replacement → governance test **fails**
- ⚠ `MudButton Color.Error` → advisory (does not fail; consider `ga-btn-danger` for destructive actions)

---

## Core Rule

```
blazor-page-pattern.md in backend/patterns/ is the authority for page LIFECYCLE.
This folder (ui/) is the authority for page STRUCTURE, COMPONENTS, TOKENS, and UI MODELS.
```
