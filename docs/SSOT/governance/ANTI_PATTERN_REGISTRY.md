# ANTI_PATTERN_REGISTRY

```yaml
id: anti_pattern_registry
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/ANTI_PATTERN_REGISTRY.md

purpose: >
  Central registry of confirmed anti-patterns. Each entry was produced by an actual
  failure or incorrect generation in this codebase — not hypothetical.
  
  When AI produces code: check this file FIRST before writing handlers, queries, or tests.

rule: APPEND on every confirmed anti-pattern. Never delete entries.
```

---

## Anti-Pattern Entries

---

### APR_001 — `Result.Success()` and `Result.Failure()` do not exist

```yaml
id: APR_001
category: result_pattern
confirmed: 2026-04-03
source: handler-pattern.md contained wrong syntax
detect: >
  Code uses Result.Success(value) or Result.Failure("CODE", "msg")
why_wrong: >
  Result<T> has no static methods named Success or Failure.
  These are Java/other language conventions. GreenAi uses Ok/Fail.
fix:
  success: Result<T>.Ok(value)
  failure: Result<T>.Fail("ERROR_CODE", "human message")
  proof: src/GreenAi.Api/SharedKernel/Results/Result.cs
```

---

### APR_002 — `result.IsFailure` property does not exist

```yaml
id: APR_002
category: result_pattern
confirmed: 2026-04-03
source: ChangePasswordHandlerTests.cs initial version
detect: >
  Code asserts result.IsFailure or branches on result.IsFailure
why_wrong: >
  Result<T> has only IsSuccess: bool.
  IsFailure does not compile.
fix:
  check_failure: Assert.False(result.IsSuccess) or if (!result.IsSuccess)
  access_error: result.Error!.Code  (non-null when !IsSuccess)
  proof: src/GreenAi.Api/SharedKernel/Results/Result.cs
```

---

### APR_003 — `SqlLoader` injected as constructor parameter

```yaml
id: APR_003
category: dapper_pattern
confirmed: 2026-04-03
source: handler-pattern.md contained wrong syntax showing SqlLoader as injected
detect: >
  Handler constructor: public MyHandler(SqlLoader sql, ...)
  or field: private readonly SqlLoader _sql;
why_wrong: >
  SqlLoader is a static class with no instance.
  It cannot be registered in DI or injected.
fix:
  use: SqlLoader.Load<T>("File.sql") — called directly in method body
  proof: src/GreenAi.Api/SharedKernel/Db/SqlLoader.cs (static class)
```

---

### APR_004 — `OnInitializedAsync` for auth in Blazor pages

```yaml
id: APR_004
category: blazor_page
confirmed: 2026-04-03
source: blazor-page-pattern.md documents this explicitly
detect: >
  Blazor page loads user identity or calls Mediator.Send in OnInitializedAsync
why_wrong: >
  Blazor Server prerenders before WebSocket circuit is established.
  During prerender: GreenAiAuthenticationStateProvider returns Unauthenticated.
  JS interop (localStorage auth token) is unavailable during prerender.
  OnInitializedAsync fires during prerender → always sees unauthenticated state.
fix:
  use: OnAfterRenderAsync(bool firstRender) — only fires in interactive circuit
  pattern: docs/SSOT/backend/patterns/blazor-page-pattern.md
```

---

### APR_005 — Manual auth check instead of `IRequireAuthentication` marker

```yaml
id: APR_005
category: pipeline_behavior
confirmed: 2026-04-03
source: All 3 CustomerAdmin handlers had this pattern
detect: >
  Handler body contains:
    if (!user.IsAuthenticated || !HasCustomerId()) return Result.Fail("NO_CUSTOMER", ...)
  or:
    private bool HasCustomerId() { try { _ = user.CustomerId; return true; } catch { return false; } }
why_wrong: >
  AuthorizationBehavior pipeline behavior already enforces authentication via
  IRequireAuthentication marker. Manual check is duplicate logic that can diverge.
  HasCustomerId() try/catch is a symptom of missing pipeline marker.
  When IRequireAuthentication is applied, user.CustomerId is guaranteed valid — no check needed.
fix:
  command_or_query: add IRequireAuthentication to interface declaration
  handler: remove manual check and HasCustomerId() method entirely
  using: using GreenAi.Api.SharedKernel.Pipeline;
  pattern: docs/SSOT/backend/patterns/pipeline-behaviors.md
  decision_table: see pipeline-behaviors.md for when to apply each marker
```

---

### APR_006 — `endpoint.Map(WebApplication app)` signature

```yaml
id: APR_006
category: endpoint_pattern
confirmed: 2026-04-03
source: endpoint-pattern.md v1 showed WebApplication, actual code uses IEndpointRouteBuilder
detect: >
  Endpoint.Map method signature: public static void Map(WebApplication app)
why_wrong: >
  WebApplication works but couples the endpoint to the concrete host type.
  IEndpointRouteBuilder is the correct interface — enables testability and
  route group registration. All actual endpoints in codebase use IEndpointRouteBuilder.
fix:
  use: public static void Map(IEndpointRouteBuilder app)
  proof: src/GreenAi.Api/Features/Auth/ChangePassword/ChangePasswordEndpoint.cs
```

---

### APR_007 — Inline HTTP status logic in endpoints

```yaml
id: APR_007
category: endpoint_pattern
confirmed: 2026-04-03
source: endpoint-pattern.md v1 showed inline Results.BadRequest() / Results.Created()
detect: >
  Endpoint returns inline status:
    return result.IsSuccess ? Results.Created(...) : Results.BadRequest(result.Error)
  or any variant: Results.Ok(), Results.NotFound(), Results.Problem() inline
why_wrong: >
  All HTTP status mapping is owned by ResultExtensions.ToHttpResult().
  Inline logic creates a second mapping that can diverge from the canonical mapping.
  ResultExtensions is the single source of truth for error code → HTTP status.
fix:
  use: return result.ToHttpResult();
  proof: src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs
  pattern: docs/SSOT/backend/patterns/endpoint-pattern.md
  pattern: docs/SSOT/backend/patterns/result-pattern.md
```

---

### APR_008 — Inline SQL string in handler or repository

```yaml
id: APR_008
category: dapper_pattern
confirmed: 2026-04-03
source: RED_THREAD sql_embedded; hypothetical but critical security anti-pattern
detect: >
  Handler or repository passes string literal to QueryAsync/ExecuteAsync:
    await _db.QueryAsync<T>("SELECT * FROM Users WHERE Id = @Id", ...)
why_wrong: >
  Violates RED_THREAD sql_embedded.
  Prevents discoverability: SQL cannot be found by searching .sql files.
  If concatenation ever creeps in: SQL injection vector.
fix:
  move SQL to: Features/[Domain]/[Feature]/[Name].sql
  load via: SqlLoader.Load<T>("Name.sql")
  csproj: Features/**/*.sql already included as EmbeddedResource — no config needed
```

**Last Updated:** 2026-04-03
