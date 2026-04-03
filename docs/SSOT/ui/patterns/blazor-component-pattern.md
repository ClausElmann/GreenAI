# blazor-component-pattern

```yaml
id: blazor_component_pattern
type: pattern
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/ui/patterns/blazor-component-pattern.md
related:
  - docs/SSOT/backend/patterns/blazor-page-pattern.md   (page lifecycle authority)
  - docs/SSOT/ui/patterns/mudblazor-conventions.md
```

---

## purpose

Standard pattern for reusable Blazor components that receive data via parameters.
Components MUST NOT access ICurrentUser, IMediator, or auth state directly.
All data flows IN via `[Parameter]`, all events flow OUT via `EventCallback`.

---

## rules

```yaml
MUST:
  - Receive all data via [Parameter] attributes
  - Emit changes via EventCallback<T> (never mutate parent state directly)
  - Be stateless where possible — compute display values from parameters
  - Use @code block only for local UI state (expanded/collapsed, error message)
  - File location: src/GreenAi.Api/Components/{Category}/{Name}.razor

MUST NOT:
  - Inject IMediator (call Mediator.Send in parent page, pass result down)
  - Inject BlazorPrincipalHolder (auth belongs to page, not component)
  - Use OnAfterRenderAsync for data loading (parent page owns data lifecycle)
  - Use @page directive (components are not routable)
  - Contain business logic (validation rules, error code interpretation)
```

---

## golden_sample

```csharp
// src/GreenAi.Api/Components/Pages/CustomerAdmin/UserDetail.razor
// Shows: [Parameter] for data, no Mediator injection, MudBlazor layout only
```

---

## contracts

```yaml
parameter_contract:
  code: |
    @code {
        [Parameter, EditorRequired] public UserDetailResponse User { get; set; } = null!;
        [Parameter] public EventCallback OnProfileAdded { get; set; }
    }

  rules:
    - EditorRequired on all non-optional parameters
    - Nullable reference type null! for required reference parameters
    - EventCallback (no T) for void events, EventCallback<T> for value events

local_state_ok:
  - bool _expanded = false;
  - string? _errorMessage;
  - MudForm _form = null!;

forbidden_injections:
  - IMediator
  - BlazorPrincipalHolder
  - ICurrentUser (interface)
  - HttpContext
```

---

## anti_patterns

```yaml
- detect: "@inject IMediator Mediator" in a component (non-page .razor)
  why_wrong: Data loading is the page's responsibility — breaks single-responsibility
  fix: Move Mediator.Send to parent page, pass result as [Parameter]

- detect: "@inject BlazorPrincipalHolder" in a component
  why_wrong: Auth is established at page level — component should only receive resolved data
  fix: Parent page calls PrincipalHolder.Set(), passes UserId/ProfileId as [Parameter] if needed

- detect: @page directive in a component under Components/ folder
  why_wrong: Components are not routable — use Pages/ folder for routable pages
  fix: Move to Components/Pages/{Area}/ or remove @page directive
```

---

## enforcement

```yaml
- where: scripts/governance/Validate-GreenAiCompliance.ps1
  how: Scan Components/**/*.razor for "@inject IMediator" / "@inject BlazorPrincipalHolder"
```

**Last Updated:** 2026-04-03
