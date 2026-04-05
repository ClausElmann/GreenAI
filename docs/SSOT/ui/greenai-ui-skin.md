# GreenAI Enterprise UI Skin

**SSOT for:** colors, typography, spacing, accessibility, component skin rules.  
**Last updated:** 2026-04-05

---

## 1. Design Principles

| Goal | Rule |
|---|---|
| Professional / trustworthy | No gradients, no glassmorphism, no SaaS gimmicks |
| High-contrast | WCAG AA on all text, WCAG 3:1 on UI boundaries |
| Efficient density | Enterprise row height, compact controls |
| Municipal accessibility | Full keyboard nav, 44px touch targets mobile |
| Calm | Single primary accent, no competing colours |

---

## 2. Token System

Defined in `wwwroot/app.css` `:root`. **Always use tokens — never hardcode hex values.**

### Colors

| Token | Value | Contrast on white |
|---|---|---|
| `--ga-bg` | `#F7F9FC` | — |
| `--ga-surface` | `#FFFFFF` | — |
| `--ga-surface-alt` | `#F1F4F8` | — |
| `--ga-border` | `#D7DEE7` | — |
| `--ga-text` | `#16202A` | 15.5:1 ✅ |
| `--ga-text-muted` | `#4B5B6B` | 5.9:1 ✅ |
| `--ga-text-disabled` | `#9AA0A6` | 3.0:1 — UI-only, never body text |
| `--ga-primary` | `#0B5FFF` | 4.6:1 ✅ |
| `--ga-primary-hover` | `#084FD6` | — |
| `--ga-primary-contrast` | `#FFFFFF` | — |
| `--ga-success` | `#117A37` | 5.1:1 ✅ |
| `--ga-warning` | `#A15C00` | 4.5:1 ✅ |
| `--ga-danger` | `#B42318` | 5.2:1 ✅ |
| `--ga-info` | `#005E7A` | 6.1:1 ✅ |
| `--ga-focus` | `#111827` | near-black |

### Spacing (4px base grid)

| Token | Value |
|---|---|
| `--ga-space-1` | 4px |
| `--ga-space-2` | 8px |
| `--ga-space-3` | 12px |
| `--ga-space-4` | 16px |
| `--ga-space-5` | 24px |
| `--ga-space-6` | 32px |
| `--ga-space-7` | 40px |

**Rules:**  
- Control gap = `--ga-space-3` (12px) or `--ga-space-4` (16px)  
- Section separator = `--ga-space-5` (24px)  
- Page section gap = `--ga-space-6` (32px)

### Typography Scale

| Role | Size | Weight | Token |
|---|---|---|---|
| Page title (AppShell) | 24px | 700 | `--ga-font-2xl` via `[data-testid="page-title"]` |
| Section title | 20px | 600 | `--ga-font-xl` / `Typo.h6` |
| Card title | 16px | 600 | `--ga-font-lg` / `Typo.h5` |
| Body | 14px | 400 | `--ga-font-sm` / `--ga-font-base` |
| Small / help text | 12px | 400 | `--ga-font-xs` |
| Stat metric (large) | 28px | 700 | `--ga-font-2xl` / `Typo.h4` |

Font stack: `'Roboto', 'Inter', 'Helvetica Neue', Arial, sans-serif`

### Border Radius

| Token | Value | Use |
|---|---|---|
| `--ga-radius-sm` | 6px | Chips, badges |
| `--ga-radius-md` | 8px | Cards, buttons, inputs |
| `--ga-radius-lg` | 10px | Dialogs, command palette |

### Elevation (restrained)

| Token | Value | Use |
|---|---|---|
| `--ga-shadow-card` | `0 1px 2px rgba(…,.06), 0 1px 3px rgba(…,.10)` | Cards, papers |
| `--ga-shadow-overlay` | `0 8px 24px rgba(…,.12), …` | Panels, drawers |
| `--ga-shadow-palette` | `0 8px 32px rgba(…,.18), …` | Command palette, dialogs |

### Z-index Stack

| Token | Value |
|---|---|
| `--ga-z-topbar` | 100 |
| `--ga-z-overlay` | 1300 |
| `--ga-z-palette` | 1500 |
| `--ga-z-modal` | 1600 |

### Layout

| Token | Value |
|---|---|
| `--ga-content-max` | 1200px |
| `--ga-form-max` | 480px |
| `--ga-topbar-height` | 56px |

---

## 3. MudBlazor Palette Override

Defined in `wwwroot/css/greenai-skin.css`. Maps `--ga-*` tokens to `--mud-palette-*`.  
This file is loaded **after** `MudBlazor.min.css` in `App.razor`.

Key mappings:
- `--mud-palette-primary` → `#0B5FFF`
- `--mud-palette-background` → `#F7F9FC`
- `--mud-palette-surface` → `#FFFFFF`
- `--mud-palette-divider` → `#D7DEE7`
- `--mud-palette-text-primary` → `#16202A`
- etc. (see file for complete list)

---

## 4. Load Order (App.razor)

```
1. bootstrap.min.css   (reset / grid)
2. app.css             (--ga-* tokens + structural base)
3. GreenAi.Api.styles.css  (component-scoped CSS bundles)
4. MudBlazor.min.css   (component library)
5. greenai-skin.css    (--mud-palette-* overrides, component skin)  ← MUST be last
```

---

## 5. Component Skin Rules

### TopBar
- Height: `var(--ga-topbar-height)` (56px)
- Background: `var(--ga-surface)` with `1px solid var(--ga-border)` bottom
- No shadow — border provides separation
- Icon buttons: `var(--ga-text)` colour, hover = `var(--ga-surface-alt)` bg
- CSS file: `Components/Layout/TopBar.razor.css`

### OverlayNav
- Backdrop: `rgba(15, 23, 42, 0.68)` (near-dark, no pure black)
- Panel: `var(--ga-surface)`, `var(--ga-shadow-overlay)`, 280px wide
- Active item: `rgba(11,95,255,.10)` bg + `var(--ga-primary)` text + `font-weight: 600`
- Active item left accent bar: 3px solid primary, 60% height
- Touch target: 44px min-height on mobile
- CSS file: `Components/Layout/OverlayNav.razor.css`

### CommandPalette
- Backdrop: `rgba(15, 23, 42, 0.68)` + `blur(4px)`
- Panel: `min(600px, 92vw)`, positioned at 14% from top
- Elevation: `var(--ga-shadow-palette)`
- **Mobile fullscreen** (≤599px): no border-radius, full dvh
- Active item: `var(--ga-primary)` text, `--mud-palette-action-default-hover` bg
- CSS file: `Components/Layout/CommandPalette.razor.css`

### Buttons
- Primary: `var(--ga-primary)` fill, white text, `min-height: 40px` (44px mobile)
- Secondary / Outlined: `var(--ga-border)` border, `var(--ga-text)` text, `var(--ga-surface)` bg
- Text/Ghost: `var(--ga-primary)` text, no border
- Danger: `var(--ga-danger)` fill, white text
- `text-transform: none` on all buttons (no AllCaps)
- `border-radius: var(--ga-radius-md)`
- Icon buttons: `border-radius: 50%`, no min-height override

### Forms / Inputs
- Border: `1px solid var(--ga-border)`, radius: `var(--ga-radius-md)`
- Min-height: 40px desktop, 44px mobile
- Hover border: `var(--ga-text-muted)`
- Focus: visible via global `*:focus-visible` rule (see §6)

### Cards / Papers
- Border: `1px solid var(--ga-border)`
- Shadow: `var(--ga-shadow-card)`
- Radius: `var(--ga-radius-md)`
- No glow, no heavy shadow

### Tables
- Header: `var(--ga-font-xs)` / 600 / uppercase / `var(--ga-text-muted)`, bg `var(--ga-bg)`, `2px border-bottom`
- Row: `8px 12px` padding, `1px solid var(--ga-border)` bottom
- Zebra: even rows `var(--ga-surface-alt)`
- Row hover: `rgba(11,95,255,.04)` background
- Numeric columns: text-align right

### Chips
- Height: 24px, radius: `var(--ga-radius-sm)`, size: `var(--ga-font-xs)`

### Tabs
- Text: `var(--ga-text-muted)`, active = `var(--ga-primary)` + `font-weight: 600`
- Slider: `var(--ga-primary)`
- `text-transform: none`

---

## 6. Accessibility Requirements

### Focus Ring (WCAG 2.4.11)
```css
/* app.css — global */
*:focus-visible {
    outline: 3px solid var(--ga-focus);
    outline-offset: 2px;
}
```
- `var(--ga-focus)` = `#111827` (near-black, max contrast)
- Applied globally — NOT removed for mouse users (`:focus-visible` selector ensures no visible ring on click)
- MudBlazor interactive elements explicitly listed in `greenai-skin.css`

### Touch Targets (WCAG 2.5.5)
- All buttons: `min-height: 40px` desktop, `min-height: 44px` mobile (≤599px)
- Command palette results: `padding: 10px 16px` (implicit 40px+ height)
- Nav items: `44px min-height` on mobile
- Icons used as buttons: `min 44×44 tap area` on mobile

### Color: Not Sole Meaning
- Status badges: always include text label alongside colour
- Error states: colour + icon + text message
- All icons in nav: label visible on desktop, aria-label on mobile

### Keyboard Navigation
- All interactive elements reachable via Tab
- No `tabindex="-1"` unless genuinely decorative
- Modal/dialog traps focus (MudBlazor handles this natively)
- CommandPalette: keyboard-navigable results list (↑/↓ arrows)

---

## 7. Status Badge Classes

Apply to `MudChip` or any `span`/`div`:

| Status | CSS Class |
|---|---|
| Draft | `ga-status-draft` |
| PendingApproval | `ga-status-pending` |
| Approved | `ga-status-approved` |
| Scheduled | `ga-status-scheduled` |
| Sending | `ga-status-sending` |
| Sent | `ga-status-sent` |
| Failed | `ga-status-failed` |
| Rejected | `ga-status-rejected` |
| Unknown | `ga-status-unknown` |

Pattern: `rgba(color,.10)` background + full-saturated text + `rgba(color,.25)` border.  
WCAG: all text colours ≥ 4.5:1 on white.

---

## 8. Utility Classes

| Class | Use |
|---|---|
| `.ga-form-page` | Centered auth/login page layout |
| `.ga-form-card` | Constrained form card (`max-width: 480px`) |
| `.select-context-page` | Profile/customer selection full-page |
| `.select-context-grid` | Grid container (`max-width: 640px`) |
| `.select-context-card` | Selectable card with hover/focus |
| `.ga-page-container` | Page content container (1200px max, auto margins) |

---

## 9. Responsive Rules

| Breakpoint | Rules |
|---|---|
| ≥ 960px (desktop) | Dense layout, multi-column, full TopBar |
| 600–959px (tablet) | 2-col where sensible, same density |
| ≤ 599px (mobile) | 1-col only, 44px touch targets, CommandPalette fullscreen, sticky wizard footer |

---

## 10. What Is Not Skinned (intentional)

- **MudBlazor component JavaScript** — unchanged
- **Routing / navigation logic** — unchanged
- **Business data display** — unchanged
- **MudThemeProvider C# config** — not used (CSS-only approach)

---

## 11. Files

| File | Role |
|---|---|
| `wwwroot/app.css` | `--ga-*` tokens + base reset |
| `wwwroot/css/greenai-skin.css` | MudBlazor component skin (loaded after MudBlazor) |
| `Components/App.razor` | CSS load order (greenai-skin.css added after MudBlazor) |
| `Components/Layout/MainLayout.razor.css` | Main content padding |
| `Components/Layout/TopBar.razor.css` | TopBar scoped styles |
| `Components/Layout/OverlayNav.razor.css` | Nav panel + backdrop |
| `Components/Layout/CommandPalette.razor.css` | Palette modal + mobile fullscreen |
| `Components/Pages/Auth/SelectCustomerPage.razor.css` | Card interaction overrides |
| `Components/Pages/Auth/SelectProfilePage.razor.css` | Card interaction overrides |
| `Components/Pages/Broadcasting/BroadcastingHubPage.razor.css` | Hub send-method card |
| `Components/Pages/Broadcasting/SendWizardPage.razor.css` | Wizard step indicator + method card |
| `Components/Pages/Broadcasting/StatusDetailPage.razor.css` | Stat cards |
