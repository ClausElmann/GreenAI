# Green AI — UI Architecture Decisions

```yaml
id: ui_architecture_decisions
type: decisions
version: 1.0.0
last_updated: 2026-04-05
ssot_source: docs/SSOT/ui/ui-architecture-decisions.md
status: LOCKED
locked_date: 2026-04-05
```

> **LOCKED** — these decisions govern all UI work.
> Changes require explicit architect approval and update of this file.

---

## 1. Routing

### Root redirect (`/`)

| Condition | Target |
|---|---|
| Unauthenticated | `/login` |
| Authenticated, multiple customers | `/select-customer` |
| Authenticated, single customer, multiple profiles | `/select-profile` |
| Authenticated, single customer, single profile | `/broadcasting` |

### `/dashboard` status: **DEPRECATED**

`/dashboard` is deprecated and redirects immediately to `/broadcasting`.  
All nav links, breadcrumbs, and internal references must target `/broadcasting`.

---

## 2. Primary Navigation

Entry point for all authenticated users: **`/broadcasting`**

| Route | Label key | Role requirement | Nav visible |
|---|---|---|---|
| `/broadcasting` | `nav.Broadcasting` | any authenticated | ✅ |
| `/status` | `nav.Status` | any authenticated | ✅ |
| `/drafts` | `nav.Drafts` | any authenticated | ✅ |
| `/user/profile` | `nav.UserProfile` | any authenticated | ✅ |
| `/customer-admin` | `nav.CustomerAdmin` | CustomerSetup, ManageUsers, ManageProfiles | ✅ (role-gated) |
| `/admin/users` | `nav.AdminUsers` | SuperAdmin, ManageUsers | ✅ (role-gated) |
| `/admin/settings` | `nav.AdminSettings` | SuperAdmin | ✅ (role-gated) |
| `/admin/super` | `nav.SuperAdmin` | SuperAdmin | ✅ (role-gated) |

Nav: **NO dashboard entry.** Broadcasting is the home.

---

## 3. Broadcasting Status Model

Broadcasts carry exactly one of these statuses:

| Status | Meaning |
|---|---|
| `Draft` | Saved in wizard, not dispatched |
| `PendingApproval` | Submitted, awaiting ApproveBroadcast role sign-off |
| `Approved` | Approved, ready to send or schedule |
| `Scheduled` | Approved, queued for future dispatch |
| `Sending` | Actively being dispatched |
| `Sent` | All messages dispatched |
| `Failed` | Dispatch failed partially or fully |
| `Rejected` | Rejected by an approver |

---

## 4. Send Wizard

### Steps (fixed, 4)

1. **Method** — select one of 6 send methods
2. **Recipients** — UI varies per method
3. **Message** — compose text, sender-ID, channel
4. **Confirm** — summary + send-now vs. schedule

### Auto-save (REQUIRED)

Auto-save to `AppState.DraftWizardState` triggers on:
- Step change (next/back)
- Route change (NavigationManager.LocationChanged)

Draft is stored as a mock JSON blob in `AppState`. On reload from `/drafts`, wizard restores from state.

### Scenario = wizard prefill

Clicking a Scenario on BroadcastingHubPage navigates to `/send/wizard?scenario={id}`.  
Wizard reads `?scenario` from query string and prefills all 4 steps with Scenario mock data.

---

## 5. Status Page

### Tabs (primary navigation within page)

| Tab | Shows | Filter |
|---|---|---|
| **Sent** | Broadcasts with status `Sent` | date range + search |
| **Scheduled** | `Scheduled` | date range + search |
| **Failed** | `Failed` + `Rejected` | date range + search |

Tabs are the primary filter. Date range and search apply within the active tab.

---

## 6. Drafts

- Draft stores full wizard state (mock JSON in AppState / static MockData)
- Deleted on: successful send, manual delete
- Clicking draft opens wizard at step 1 with state pre-loaded

---

## 7. Roles

| Role | Capability |
|---|---|
| `CanBroadcast` | Can use send wizard, manage own broadcasts |
| `ApproveBroadcast` | Can approve/reject PendingApproval broadcasts |
| `ManageUsers` | Admin: user CRUD |
| `ManageProfiles` | Admin: profile CRUD |
| `CustomerSetup` | Admin: customer settings |
| `SuperAdmin` | All of the above + super-admin context page |

Broadcasts that require approval: determined by customer settings (mock: always required for large recipient counts > 1000).

---

## 8. Select Customer / Select Profile

- **`/select-customer`** — shown when authenticated user has access to > 1 customer. Shows card list. Selecting sets `AppState.ActiveCustomerName` and continues.
- **`/select-profile`** — shown after customer is selected, when that customer has > 1 profile. Shows card list. Selecting sets `AppState.ActiveProfileName` and continues.
- Both pages use `EmptyLayout` (no TopBar/nav chrome).
- In mock: `MockData.Customers` and `MockData.Profiles` drive the lists.

---

## 9. Command Palette (Ctrl+K)

- Triggered by: `Ctrl+K` keyboard shortcut (global), button in TopBar
- Opens as : MudOverlay + centered MudPaper
- Searches across: all nav items in `AppState.AllowedNavItems` + recent broadcasts from MockData
- Results filtered by: role (only allowed routes), text match
- Navigate on Enter, close on Escape
- On mobile: full-screen
