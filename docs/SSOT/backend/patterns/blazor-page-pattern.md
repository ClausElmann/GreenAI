# blazor-page-pattern

```yaml
id: blazor_page_pattern
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/blazor-page-pattern.md
red_thread: current_user + result_pattern → docs/SSOT/governance/RED_THREAD_REGISTRY.md

golden_sample: src/GreenAi.Api/Components/Pages/CustomerAdmin/Index.razor

purpose: >
  Standard pattern for authenticated Blazor pages that call MediatR handlers.
  OnAfterRenderAsync MUST be used (not OnInitializedAsync) because Blazor Server
  prerenders components before the WebSocket circuit is established.
  During prerender, JS interop (localStorage → auth token) is unavailable.

lifecycle_rule:
  use: OnAfterRenderAsync(bool firstRender)
  not: OnInitializedAsync
  reason: Prerender runs before circuit — GreenAiAuthenticationStateProvider always returns
          Unauthenticated during prerender. OnAfterRenderAsync only fires in interactive circuit.

required_fields:
  - "[CascadingParameter] Task<AuthenticationState> AuthStateTask"
  - "bool _loading = true"
  - "[Inject] BlazorPrincipalHolder PrincipalHolder"
  - "[Inject] IMediator Mediator"
  - "[Inject] NavigationManager Nav"

flow:

  - step: 1_guard_first_render
    action: "if (!firstRender) return;"
    why: OnAfterRenderAsync fires on every render — only the first matters for init

  - step: 2_get_auth_state
    action: "var authState = await AuthStateTask;"

  - step: 3_auth_guard
    action: "if (!authenticated) { Nav.NavigateTo(\"/login\"); return; }"
    detect: authState.User.Identity?.IsAuthenticated != true

  - step: 4_set_principal
    action: "PrincipalHolder.Set(authState.User);"
    rule: MUST precede ANY Mediator.Send call — this makes ICurrentUser work in handlers

  - step: 5_load_data
    action: "var result = await Mediator.Send(new MyQuery());"

  - step: 6_handle_no_customer
    action: |
      if (!result.IsSuccess && result.Error?.Code == "NO_CUSTOMER")
      {
          Nav.NavigateTo("/select-customer");
          return;
      }
    rule: Only required on pages that need CustomerId. Skip if page is pre-tenant.

  - step: 7_assign_state
    action: assign result.Value to component fields

  - step: 8_end_loading
    action: "_loading = false; StateHasChanged();"
    rule: StateHasChanged MUST be called after setting _loading = false in OnAfterRenderAsync
          because the render that uses the loaded data is triggered manually here

contracts:

  - name: page_template
    code: |
      @page "/my-route"
      @inject IMediator Mediator
      @inject NavigationManager Nav
      @inject BlazorPrincipalHolder PrincipalHolder

      @code {
          [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

          private bool _loading = true;
          // ... data fields

          protected override async Task OnAfterRenderAsync(bool firstRender)
          {
              if (!firstRender) return;

              var authState = await AuthStateTask;
              if (authState.User.Identity?.IsAuthenticated != true)
              {
                  Nav.NavigateTo("/login");
                  return;
              }

              PrincipalHolder.Set(authState.User);

              var result = await Mediator.Send(new MyQuery());

              if (!result.IsSuccess && result.Error?.Code == "NO_CUSTOMER")
              {
                  Nav.NavigateTo("/select-customer");
                  return;
              }

              if (result.IsSuccess)
                  _myData = result.Value!;

              _loading = false;
              StateHasChanged();
          }
      }

  - name: loading_ui_template
    code: |
      @if (_loading)
      {
          <MudProgressCircular Indeterminate="true" Class="mt-8" />
      }
      else
      {
          // page content
      }

anti_patterns:

  - detect: auth logic in OnInitializedAsync
    why_wrong: prerender makes authState always Unauthenticated — redirect fires on every page load
    fix: move entire init block to OnAfterRenderAsync(bool firstRender)

  - detect: Mediator.Send called before PrincipalHolder.Set
    why_wrong: ICurrentUser.CustomerId throws — handler gets no identity context
    fix: PrincipalHolder.Set(authState.User) MUST be step 4 before any Send

  - detect: StateHasChanged missing after _loading = false
    why_wrong: page stays in loading spinner forever — Blazor doesn't re-render automatically
               after async work in OnAfterRenderAsync
    fix: always call StateHasChanged() as last statement in OnAfterRenderAsync

  - detect: data-testid attributes absent on key page elements
    why_wrong: E2E tests cannot reliably locate elements by text (localisation changes text)
    fix: add data-testid="page-name-heading" etc. to all heading + interactive elements

enforcement:

  - where: Components/Pages/**/*.razor with auth logic
    how: must use OnAfterRenderAsync — grep: "OnInitializedAsync" in pages with [CascadingParameter]

  - where: Components/Pages/**/*.razor
    how: PrincipalHolder.Set must appear before first Mediator.Send in code block
```
