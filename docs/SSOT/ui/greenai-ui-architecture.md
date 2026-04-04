# Green AI — UI Architecture

> **Status:** IMPLEMENTED — UI shell slice complete (AppState, TopBar, OverlayNav, MainLayout refactor).
> **Scope:** Target state for Green AI Blazor UI.
> **Last Updated:** 2026-04-04
> **Decisions locked:** 2026-04-04 (routing + wizard draft + implementation order)

---

## 1. Product Context

Green AI is a **targeted SMS portal** — not a generic product.

Primary workflows:
- Send messages (core)
- View delivery status
- Manage drafts
- Admin (light: users, roles, settings)

This informs every UI decision: no chrome that doesn't serve these 4 workflows.

---

## 2. Layout Structure

```
AppShell
├── TopBar                     (persistent — 48px)
│   ├── Brand / Logo
│   ├── ContextIndicator       (active customer/profile)
│   ├── [Ctrl+K] CommandPalette trigger
│   └── UserMenu               (avatar + role + logout)
├── OverlayNav                 (overlay-only — NOT sidebar)
│   ├── Primary actions        (role-aware)
│   └── Close trigger          (ESC / click-outside)
├── CommandPalette             (Ctrl+K / modal)
│   ├── Search routes
│   ├── Search drafts
│   └── Quick actions
└── RouterOutlet               (100% of remaining viewport)
```

**Key constraint:** `RouterOutlet` always has 100% viewport minus TopBar (48px).  
No sidebar consumes screen real estate — ever.

---

## 3. Navigation Model

### 3.1 Primary Navigation — Overlay Only

Navigation is accessed via:
1. Hamburger menu in TopBar → `OverlayNav` slides over content
2. `Ctrl+K` → `CommandPalette` modal
3. Direct URL navigation (deep links always work)

The overlay MUST close on:
- Route navigation
- ESC key
- Click-outside overlay

### 3.2 Route Hierarchy

```
/                   → redirect → /dashboard (authenticated) or /login (unauthenticated)
/login              → EmptyLayout — no TopBar  [KEEP UNCHANGED]
/select-customer    → EmptyLayout — no TopBar
/select-profile     → EmptyLayout — no TopBar
/dashboard          → MainLayout — intent-based entry point  [REPLACES Home.razor]
/send/wizard        → WizardLayout — route placeholder now; full flow later
/send/wizard/:step  → WizardLayout — step 1..N
/status             → MainLayout — tabs: Sent / Scheduled / Failed
/drafts             → MainLayout — list + edit drawer
/admin              → MainLayout — SuperAdmin only (redirect if missing role)
/admin/users        → MainLayout — SuperAdmin/ManageUsers
/admin/settings     → MainLayout — SuperAdmin only
/user/profile       → MainLayout — self: any authenticated user
/customer-admin     → MainLayout — CustomerSetup/ManageProfiles/ManageUsers
```

### 3.3 Routing Decisions (LOCKED)

**`/` root route:**
- Authenticated users: MUST redirect to `/dashboard`
- Unauthenticated users: redirect to `/login` (existing auth flow handles this)
- `Home.razor` is **deprecated** — must not remain as a user-facing page
- Deprecation action: convert to redirect-only or delete in same slice as `/dashboard` creation

**`/login`:**
- `EmptyLayout` MUST remain unchanged
- No changes to auth flow in this slice

### 3.3 Layout Variants

| Layout | Components | Usage |
|--------|-----------|-------|
| `EmptyLayout` | None | Login, OAuth callbacks, wizard |
| `MainLayout` | TopBar + OverlayNav | All authenticated pages |
| `WizardLayout` | TopBar (minimal) + wizard progress bar | Multi-step send flow |

---

## 4. Role Visibility Rules

| Route | Required Roles | Redirect if missing |
|-------|---------------|---------------------|
| `/dashboard` | any authenticated | `/login` |
| `/send/wizard` | any authenticated | `/login` |
| `/status` | any authenticated | `/login` |
| `/drafts` | any authenticated | `/login` |
| `/admin` | `SuperAdmin` | `/dashboard` |
| `/admin/users` | `SuperAdmin` OR `ManageUsers` | `/dashboard` |
| `/admin/settings` | `SuperAdmin` | `/dashboard` |
| `/customer-admin` | `CustomerSetup` OR `ManageUsers` OR `ManageProfiles` | `/dashboard` |

**Navigation visibility:** OverlayNav renders only links the current user has access to.  
**Direct URL access:** Server-side auth check (redirect on 403/401).

---

## 4a. Wizard Draft Behavior (LOCKED)

Wizard (`/send/wizard`) MUST auto-save to draft on navigation away.

**Required save triggers (non-negotiable):**
- Step change (any forward/back step navigation within wizard)
- Explicit "Back" button click
- Route/navigation away (browser back, OverlayNav link, TopBar link)

**Out of scope for current slice:**
- Debounced autosave while typing (nice-to-have, later)
- Collaborative/live autosave
- Conflict resolution

**Recovery model:**
- Draft saved to `/drafts` — user can resume from list
- Auto-save is "safe recovery", not "live sync"
- Wizard may show "Kladde gemt" toast on auto-save

---

## 4b. Implementation Order (LOCKED)

```
1. AppState        (scoped service — IsNavOpen, IsCommandOpen, CurrentCustomerName)
2. TopBar.razor    (replaces MudAppBar content in MainLayout — uses AppState)
3. OverlayNav.razor (replaces MudDrawer in MainLayout — uses AppState)
4. MainLayout refactor (wire TopBar + OverlayNav, remove MudDrawer Responsive)
```

**Explicit constraints:**
- `EmptyLayout` for `/login`: DO NOT TOUCH
- `CommandPalette`: DO NOT build before TopBar + OverlayNav shell is stable
- `/send/wizard`: implement route placeholder + WizardLayout shell ONLY — no step logic yet

---

## 5. Component Hierarchy

```
AppShell.razor                 (layout host — MudLayout wrapper)
├── TopBar.razor               (MudAppBar — role-aware content)
│   ├── NavToggleButton        (opens OverlayNav)
│   ├── BrandMark              (text/logo)
│   ├── ContextChip            (active customer → profile)
│   ├── CommandPaletteButton   (Ctrl+K)
│   └── UserMenu.razor         (MudMenu — name, role, logout)
│
├── OverlayNav.razor           (absolute positioned overlay — NOT MudDrawer permanent)
│   ├── NavSection "Send"      (dashboard / wizard)
│   ├── NavSection "Messages"  (status / drafts)
│   └── NavSection "Admin"     (AuthorizeView Roles="SuperAdmin,ManageUsers")
│
├── CommandPalette.razor       (MudDialog or custom — keyboard-first)
│   ├── SearchInput
│   ├── RouteResults
│   └── ActionResults
│
└── WizardLayout.razor         (separate layout — replaces MainLayout for /send/wizard)
    ├── WizardProgressBar      (step N of M)
    ├── WizardStepContent      (@Body)
    └── WizardNavButtons       (Back / Next / Send)
```

**Shared components** (Components/Shared/):
- `AppShell.razor` — page skeleton (heading + breadcrumb + loading + error)
- `ErrorAlert.razor` — consistent error display
- `LoadingOverlay.razor` — loading state wrapper
- `ConfirmDialog.razor` — destructive action confirmation

---

## 6. State Model

```csharp
// AppState — scoped service, injected into TopBar + OverlayNav + CommandPalette
public sealed class AppState
{
    public bool IsNavOpen       { get; }   // OverlayNav visible
    public bool IsCommandOpen   { get; }   // CommandPalette visible
    public string? CurrentCustomerName { get; }  // display label in ContextChip
    
    public void OpenNav();
    public void CloseNav();
    public void ToggleNav();
    public void OpenCommand();
    public void CloseCommand();
}
```

`AppState` is `Scoped` (per Blazor circuit).  
Components subscribe via `StateChanged` event + `StateHasChanged()`.

---

## 7. UX Principles

1. **Zero permanent sidebar** — overlay only
2. **Max screen real estate** — content is always full-width
3. **Primary action in 1 click** — "Send besked" reachable from dashboard or Ctrl+K
4. **Progressive disclosure** — default view is minimal; details on demand
5. **Wizard for complexity** — multi-step send flow uses dedicated WizardLayout
6. **Role-aware chrome** — admin links invisible to non-admin users
7. **Keyboard-first** — Ctrl+K always opens CommandPalette

---

## 8. Forbidden Patterns

```
❌ MudDrawer with Variant="Persistent" or "Responsive" (permanent sidebar)
❌ MudNavMenu embedded in layout — use OverlayNav only
❌ NavMenu.razor / NavigationMenu.razor in MainLayout — replace with TopBar + OverlayNav
❌ AppBar with hamburger that toggles a sidebar
❌ @page directives outside Components/Pages/ or Features/*/Page.razor
❌ Layout-level data loads (no Mediator.Send in MainLayout.razor)
❌ Navigation links visible to roles that cannot access the route
❌ Inline form flows replacing wizard pattern for send flow
❌ Any component that duplicates navigation (two nav menus)
❌ Home.razor as a user-facing page (deprecated — redirect only or delete)
❌ CommandPalette built before TopBar + OverlayNav shell is stable
❌ /send/wizard step implementation before layout shell is proven
❌ Changes to EmptyLayout or /login flow in this slice
```

---

## 9. Migration Path

Current repo uses:
- `MudDrawer` (sidebar, Responsive variant) in `MainLayout.razor`
- `NavigationMenu.razor` embedded in drawer

Target replaces these with:
- `TopBar.razor` in `MainLayout.razor`
- `OverlayNav.razor` opened via button in TopBar

This is a **breaking change to MainLayout** — implement in a dedicated slice, do not mix with feature work.

---

**Authority:** This file is the single source of truth for Green AI UI layout decisions.  
**Next step:** Implement AppState → TopBar → OverlayNav → CommandPalette (in that order).
