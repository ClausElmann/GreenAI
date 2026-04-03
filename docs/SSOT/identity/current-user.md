# current-user

```yaml
id: current_user
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/identity/current-user.md
red_thread: current_user → docs/SSOT/governance/RED_THREAD_REGISTRY.md

purpose: >
  ICurrentUser is the ONLY permitted runtime identity interface.
  HttpContextCurrentUser resolves claims from HTTP context OR Blazor circuit.
  BlazorPrincipalHolder bridges Blazor WebSocket circuits to MediatR handlers.

contracts:

  - name: ICurrentUser
    file: src/GreenAi.Api/SharedKernel/Auth/ICurrentUser.cs
    shape:
      UserId:           UserId       # always > 0 when IsAuthenticated
      CustomerId:       CustomerId   # always > 0 for operational requests
      ProfileId:        ProfileId    # > 0 for IRequireProfile requests (enforced by pipeline)
      LanguageId:       int
      Email:            string
      IsAuthenticated:  bool
      IsImpersonating:  bool
      OriginalUserId:   UserId?      # non-null only when impersonating

  - name: HttpContextCurrentUser
    file: src/GreenAi.Api/SharedKernel/Auth/HttpContextCurrentUser.cs
    resolution_chain:
      1: IHttpContextAccessor.HttpContext?.User   # HTTP API requests — Bearer token present
      2: BlazorPrincipalHolder.Current             # Blazor circuits — set by component
      3: throws InvalidOperationException          # neither available → programming error
    notes:
      - Properties (UserId, CustomerId etc.) throw if Principal is null or claim is missing
      - Use IsAuthenticated to guard access before reading identity properties

  - name: BlazorPrincipalHolder
    file: src/GreenAi.Api/SharedKernel/Auth/BlazorPrincipalHolder.cs
    shape:
      Set(ClaimsPrincipal): void
      Current: ClaimsPrincipal?
    purpose: >
      Blazor Server runs on SignalR WebSocket circuits, not HTTP requests.
      HttpContext is null inside OnInitializedAsync / OnAfterRenderAsync.
      A Scoped BlazorPrincipalHolder is the bridge.

di_registration:
  file: src/GreenAi.Api/Program.cs
  entries:
    - builder.Services.AddScoped<BlazorPrincipalHolder>()
    - builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>()
  scope_rule: MUST be Scoped — never Singleton (cross-circuit pollution) or Transient (holder lost)

blazor_usage_contract:
  lifecycle: OnAfterRenderAsync(bool firstRender) ONLY — never OnInitializedAsync
  reason: >
    During Blazor Server prerender, the WebSocket circuit is not yet established.
    GreenAiAuthenticationStateProvider reads localStorage via JS interop.
    JS interop is unavailable during prerender → always returns Unauthenticated.
    OnAfterRenderAsync only runs in the interactive circuit where JS IS available.
  required_order:
    1: if (!firstRender) return;
    2: var authState = await AuthStateTask;
    3: if (!authenticated) → Nav.NavigateTo("/login"); return;
    4: PrincipalHolder.Set(authState.User);        # BEFORE any Mediator.Send
    5: var result = await Mediator.Send(query);
  golden_sample: src/GreenAi.Api/Components/Pages/CustomerAdmin/Index.razor

rules:
  MUST:
    - Inject ICurrentUser in handlers — never IHttpContextAccessor
    - Inject BlazorPrincipalHolder in Blazor pages — set before Mediator.Send
    - Register both as Scoped
    - Call PrincipalHolder.Set in OnAfterRenderAsync — never OnInitializedAsync
  MUST_NOT:
    - Inject HttpContext or IHttpContextAccessor into any MediatR handler
    - Read from HttpContext.User directly outside HttpContextCurrentUser
    - Create a second auth context interface (IUserContext, IAuthContext, etc.)
    - Register BlazorPrincipalHolder as Singleton or Transient

anti_patterns:

  - detect: Handler constructor has IHttpContextAccessor parameter
    why_wrong: HttpContext is null in Blazor Server circuits
    fix: replace with ICurrentUser — resolution chain handles both HTTP and Blazor

  - detect: PrincipalHolder.Set called in OnInitializedAsync
    why_wrong: circuit not established during prerender — authState always Unauthenticated
    fix: move to OnAfterRenderAsync(bool firstRender) + guard if (!firstRender) return

  - detect: BlazorPrincipalHolder registered as Singleton
    why_wrong: same instance shared across all users/circuits → identity leakage
    fix: AddScoped<BlazorPrincipalHolder>()

  - detect: Mediator.Send called before PrincipalHolder.Set in Blazor
    why_wrong: ICurrentUser.CustomerId throws InvalidOperationException
    fix: PrincipalHolder.Set(authState.User) must precede first Mediator.Send

enforcement:

  - where: Features/**/Handler.cs
    how: constructor must NOT contain IHttpContextAccessor

  - where: Components/Pages/**/*.razor
    how: PrincipalHolder.Set must appear before first Mediator.Send in OnAfterRenderAsync

  - where: Program.cs
    how: both registrations must be Scoped
```
