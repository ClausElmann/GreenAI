# mudblazor-conventions

```yaml
id: mudblazor_conventions
type: convention
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/ui/patterns/mudblazor-conventions.md
version_locked: MudBlazor 8
```

---

## purpose

Defines which MudBlazor components are approved, how to use them consistently,
and which patterns are forbidden in green-ai.

---

## approved_components

```yaml
layout:
  - MudLayout + MudAppBar + MudDrawer + MudMainContent  (root shell only)
  - MudStack (preferred over custom flexbox div stacks)
  - MudGrid + MudItem (column layouts)
  - MudSpacer (push elements to ends)

data_display:
  - MudTable       (tabular data — always set Dense=true Hover=true)
  - MudDataGrid    (sortable/filterable grids — use only when sorting is needed)
  - MudList        (simple item lists)
  - MudChip        (status badges — always set T="string")
  - MudAvatar      (user initials — Color=Color.Primary)
  - MudBreadcrumbs (navigation path — px-0 class to align with page)

feedback:
  - MudProgressLinear Indeterminate=true  (loading state — always top of page)
  - MudAlert          (error/warning/info messages — not toasts for inline errors)
  - MudSnackbar       (via ISnackbar — transient success/action feedback only)

navigation:
  - MudTabs + MudTabPanel  (multi-section pages)
  - MudLink Href          (in-page navigation — NOT NavigationManager.NavigateTo)
  - MudButton Variant=Text StartIcon (back navigation)

forms:
  - MudForm + MudTextField + MudSelect (all form inputs)
  - MudButton Variant=Filled Color=Primary (submit)
  - MudButton Variant=Text (cancel/secondary)
```

---

## rules

```yaml
MUST:
  - Use MudProgressLinear for loading states (not spinners or custom CSS)
  - Use MudAlert for inline error display (not custom divs)
  - Always set T="string" on MudChip (MudBlazor 8 generic requirement)
  - Use Dense=true Hover=true on all MudTable instances
  - Use MudStack Spacing=2 for button groups (not manual margins)

MUST NOT:
  - Use Bootstrap classes (btn, container, row, col-*) — MudBlazor only
  - Use inline style= attributes for spacing/color (use MudBlazor props + Class=)
  - Use MudDataGrid when MudTable is sufficient (only use DataGrid for sortable columns)
  - Use JavaScript interop for UI state (use Blazor state only)
  - Mix MudBlazor icons with external icon libraries (use Icons.Material.Filled.* only)
```

---

## loading_state_contract

```razor
@if (_loading)
{
    <MudProgressLinear Indeterminate="true" />
    return;
}
```

- `return` after progress bar prevents rest of template rendering during load
- `_loading = false; StateHasChanged();` at end of OnAfterRenderAsync

---

## error_state_contract

```razor
@if (_errorMessage is not null)
{
    <MudAlert Severity="Severity.Error">@_errorMessage</MudAlert>
}
```

---

## anti_patterns

```yaml
- detect: class="btn btn-primary" or class="container" in .razor files
  why_wrong: Bootstrap utility classes — MudBlazor is the only CSS framework
  fix: Replace with MudButton Variant=Filled / MudContainer

- detect: MudChip without T="string"
  why_wrong: MudBlazor 8 MudChip is generic — compiler warning without T
  fix: Add T="string" attribute

- detect: style="margin-top: 16px" or similar inline styles
  why_wrong: Spacing should use MudBlazor Class="mt-4" or Stack Spacing
  fix: Use MudBlazor spacing utilities (mt-4, mb-2, px-0 etc.)
```

---

## enforcement

```yaml
- where: scripts/governance/Validate-GreenAiCompliance.ps1
  how: Scan *.razor for "class=\"btn " / "class=\"container" / "<MudChip " without T=
```

---

## detail_navigation_architecture

```yaml
pattern: "Page navigation for detail views"
decision: >
  green-ai uses routable pages for entity detail views.
  MudDrawer is ONLY used in MainLayout.razor for the navigation sidebar.

detail_view_pattern:
  - Entity list page: /[entity]  (e.g., /customer-admin)
  - Entity detail page: /[entity]/{id}  (e.g., /customer-admin/users/{id})
  - Navigation: MudLink Href or MudButton Href to detail page URL

NOT used:
  - Slide-in drawers for entity editing (NeeoBovisWeb LDOS pattern)
  - Modal dialogs for entity editing (except confirm-delete dialogs)
  - Inline row editing (MudTable does NOT use EditTrigger)

why:
  - Routable pages are bookmarkable and browser-history friendly
  - No iframe/Angular complexity
  - Simpler Playwright test selectors (Page.Locator, not iframe.Locator)

confirm_delete_exception:
  - Delete confirmation uses MudDialogService (IDialogService.ShowAsync)
  - Pattern: ConfirmDeleteAsync → dialog → OnDelete EventCallback
  - data-testid: dialog-confirmed-delete button inside the dialog component
```

**Last Updated:** 2026-04-04
