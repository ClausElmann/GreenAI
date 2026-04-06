# GreenAI Enterprise UI Skin

**SSOT for:** design principles, component skin rules, accessibility, responsive rules.  
**Last updated:** 2026-04-06

> **Token SSOT moved (2026-04-06):** Color + typography + spacing tokens are now in  
> `wwwroot/css/design-tokens.css` + `docs/SSOT/ui/color-system.md`.  
> Load order: `docs/SSOT/ui/color-system.md` §CSS Load Order.  
> Component classes (`.ga-*`): `docs/SSOT/ui/component-system.md`.

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

## 2. Token Reference (summary — see SSOT for values)

| System | File | Tokens |
|---|---|---|
| Color | `wwwroot/css/design-tokens.css` | `--color-primary/success/warning/error/info/text-*/bg-*/border-*` |
| Typography | `wwwroot/css/design-tokens.css` | `--font-xs` … `--font-3xl`, `--font-weight-*`, `--line-height-*` |
| Spacing | `wwwroot/css/design-tokens.css` | `--space-1` … `--space-6` |
| Legacy layout/radius/z-index | `wwwroot/app.css` | `--ga-radius-*`, `--ga-z-*`, `--ga-shadow-*`, `--ga-content-max`, `--ga-topbar-height` |
| MudTheme palette | `Components/Layout/MainLayout.razor` | `--mud-palette-*` → `var(--color-*)` |

**Rule:** Never hardcode hex. Use `var(--color-*)` for semantic values, `var(--ga-*)` for structural values.



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
