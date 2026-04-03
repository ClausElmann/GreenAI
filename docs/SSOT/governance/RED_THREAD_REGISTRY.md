# RED_THREAD_REGISTRY

```yaml
id: red_thread_registry
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/RED_THREAD_REGISTRY.md

rule: ALL CODE MUST BIND TO MINIMUM ONE RED THREAD.
rule: NO DEVIATION FROM RED THREADS WITHOUT EXPLICIT SSOT UPDATE.

red_threads:

  - id: result_pattern
    description: All handlers return Result<T>. All endpoints call .ToHttpResult().
    ssot_source: docs/SSOT/backend/patterns/result-pattern.md
    code_source:
      - src/GreenAi.Api/SharedKernel/Results/Result.cs
      - src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs
    enforced_in:
      - Features/**/Handler.cs            → must return IRequest<Result<T>>
      - Features/**/Endpoint.cs           → must call result.ToHttpResult()
      - SharedKernel/Pipeline/*Behavior.cs
    shape:
      success: Result<T>.Ok(value)
      failure: Result<T>.Fail("ERROR_CODE", "human readable message")
      http_map: src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs
    violation_action: COMPILE_ERROR — handler signature must return Result<T>

  - id: auth_flow
    description: 3-step JWT flow. login → select-customer → select-profile. ProfileId(0) never issued.
    ssot_source: docs/SSOT/identity/auth-flow.md
    code_source:
      - src/GreenAi.Api/Features/Auth/Login/LoginHandler.cs
      - src/GreenAi.Api/Features/Auth/SelectCustomer/SelectCustomerHandler.cs
      - src/GreenAi.Api/Features/Auth/SelectProfile/SelectProfileHandler.cs
    enforced_in:
      - LoginHandler → never issue ProfileId(0) in JWT
      - SelectCustomerHandler → resolves profiles
      - SelectProfileHandler → issues final JWT
    shape:
      step_1: POST /auth/login → JWT(UserId) | NeedsCustomerSelection | NeedsProfileSelection
      step_2: POST /auth/select-customer → JWT(UserId+CustomerId) | NeedsProfileSelection
      step_3: POST /auth/select-profile → JWT(UserId+CustomerId+ProfileId+LanguageId)
    violation_action: STOP — read auth-flow.md before modifying any auth feature

  - id: current_user
    description: ICurrentUser provides identity in handlers. BlazorPrincipalHolder bridges Blazor circuits.
    ssot_source: docs/SSOT/identity/current-user.md
    code_source:
      - src/GreenAi.Api/SharedKernel/Auth/ICurrentUser.cs
      - src/GreenAi.Api/SharedKernel/Auth/HttpContextCurrentUser.cs
      - src/GreenAi.Api/SharedKernel/Auth/BlazorPrincipalHolder.cs
    enforced_in:
      - Features/**/Handler.cs            → inject ICurrentUser, never IHttpContextAccessor
      - Components/Pages/**/*.razor       → call PrincipalHolder.Set(authState.User) in OnAfterRenderAsync BEFORE Mediator.Send
    shape:
      di_registration:
        scope: Scoped
        pairs: [BlazorPrincipalHolder, ICurrentUser → HttpContextCurrentUser]
      blazor_order:
        1: OnAfterRenderAsync(firstRender=true)
        2: await AuthState
        3: PrincipalHolder.Set(authState.User)
        4: Mediator.Send(...)
    violation_action: FORBIDDEN — never inject HttpContext directly in handlers

  - id: tenant_isolation
    description: ALL SQL against tenant tables MUST include WHERE CustomerId = @CustomerId.
    ssot_source: docs/SSOT/identity/README.md
    code_source:
      - src/GreenAi.Api/Features/CustomerAdmin/**/*.sql
    enforced_in:
      - Features/**/Handler.cs with DB access
      - All .sql files operating on tenant-scoped tables
    tenant_tables:
      - Profiles, ProfileUserMappings, [any table with CustomerId FK]
    pre_auth_exceptions:
      - Users, UserCustomerMemberships  → CustomerId not yet known at login
    violation_action: SECURITY_VIOLATION — reject, fix before any other work

  - id: vertical_slice
    description: One feature = one folder. Fixed file set per operation.
    ssot_source: docs/SSOT/backend/README.md
    enforced_in:
      - src/GreenAi.Api/Features/[Domain]/[Feature]/
    required_files:
      - [Feature]Command.cs   → record : IRequest<Result<T>>
      - [Feature]Handler.cs   → IRequestHandler<TCommand, TResponse>
      - [Feature]Validator.cs → AbstractValidator<TCommand>
      - [Feature]Response.cs  → output record
      - [Feature]Endpoint.cs  → static Map(WebApplication app)
      - [Feature].sql         → ONE sql file per DB operation
      - [Feature]Page.razor   → only if UI feature
    violation_action: RESTRUCTURE — move files to correct folder before proceeding

  - id: sql_embedded
    description: All SQL loaded via SqlLoader from embedded .sql resources. No inline SQL strings.
    ssot_source: docs/SSOT/database/patterns/sql-conventions.md
    code_source:
      - src/GreenAi.Api/SharedKernel/Database/SqlLoader.cs
    enforced_in:
      - Features/**/Handler.cs  → sql.Load("Features/.../Feature.sql")
    shape:
      load: var query = sql.Load("Features/[Domain]/[Feature]/[Feature].sql");
      execute: db.Connection.QuerySingleAsync<T>(query, parameters)
    violation_action: REJECT — remove inline SQL, create .sql file

  - id: strongly_typed_ids
    description: UserId, CustomerId, ProfileId — never raw int/Guid for identity values.
    ssot_source: docs/SSOT/identity/README.md
    code_source:
      - src/GreenAi.Api/SharedKernel/Identity/UserId.cs
      - src/GreenAi.Api/SharedKernel/Identity/CustomerId.cs
      - src/GreenAi.Api/SharedKernel/Identity/ProfileId.cs
    enforced_in:
      - all Commands, Responses, Handler parameters
    violation_action: REPLACE with typed ID

  - id: error_codes
    description: Error codes from canonical set only. ResultExtensions.cs is source of truth.
    ssot_source: src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs
    canonical_codes:
      400: VALIDATION_ERROR
      401: UNAUTHORIZED, INVALID_CREDENTIALS, INVALID_REFRESH_TOKEN, PROFILE_NOT_SELECTED
      403: FORBIDDEN, ACCOUNT_LOCKED, ACCOUNT_HAS_NO_TENANT, MEMBERSHIP_NOT_FOUND, PROFILE_ACCESS_DENIED
      404: PROFILE_NOT_FOUND
      500: NO_CUSTOMER, [unmapped defaults to 500]
    violation_action: STOP — check ResultExtensions.cs; add new code there if none fits

  - id: zero_warnings
    description: 0 compiler warnings after EVERY change, no exceptions.
    ssot_source: AI_WORK_CONTRACT.md
    enforced_in: [every_build]
    check_command: dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q
    violation_action: FIX_BEFORE_PROCEEDING — never leave warnings, fix in same operation

  - id: no_hardcoded_strings
    description: All UI strings via @Loc.Get(...). Never hardcoded in .razor files.
    ssot_source: docs/SSOT/localization/README.md
    enforced_in:
      - Components/**/*.razor
      - Components/Pages/**/*.razor
    violation_action: REPLACE with @Loc.Get(labelKey)

  - id: require_profile
    description: >
      Commands and queries that operate on profile-scoped data must be marked with
      IRequireProfile. This ensures the pipeline behavior RequireProfileBehavior
      rejects requests that carry ProfileId = 0 (pre-profile-selection state).
      Without this marker, a user who has not selected a profile can silently access
      the handler with ProfileId = 0 in ICurrentUser — producing corrupt data.
    ssot_source: docs/SSOT/identity/current-user.md
    code_source:
      - src/GreenAi.Api/SharedKernel/Pipeline/RequireProfileBehavior.cs
      - src/GreenAi.Api/SharedKernel/Pipeline/IRequireProfile.cs
    enforced_in:
      - Features/**/Command.cs or Query.cs → add `, IRequireProfile` to record declaration
    features_using_it:
      - Features/CustomerAdmin/GetCustomerSettings/GetCustomerSettingsQuery.cs
      - Features/CustomerAdmin/GetUsers/GetUsersQuery.cs
      - Features/CustomerAdmin/GetUsers/GetUserDetailsQuery.cs
      - Features/CustomerAdmin/GetProfiles/GetProfilesQuery.cs
      - Features/CustomerAdmin/GetProfiles/GetProfileDetailsQuery.cs
      - Features/Auth/ChangePassword/ChangePasswordCommand.cs
      - Features/Identity/ChangeUserEmail/ChangeUserEmailCommand.cs
    shape:
      correct: >
        public sealed record GetUsersQuery : IRequest<Result<List<UserRow>>>,
            IRequireAuthentication, IRequireProfile;
      incorrect: >
        public sealed record GetUsersQuery : IRequest<Result<List<UserRow>>>,
            IRequireAuthentication;  // missing IRequireProfile — users list accessible without profile
    violation_action: >
      ADD IRequireProfile to the command/query declaration.
      Check feature-contract-map.json pipeline_markers field to verify registration.
      Never omit IRequireProfile for features that scope data by CustomerId/ProfileId.
```
