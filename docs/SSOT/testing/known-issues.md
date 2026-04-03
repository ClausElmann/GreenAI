# Known Issues — Test Environment

```yaml
id: testing_known_issues
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/testing/known-issues.md

purpose: >
  Catalog of confirmed test environment traps that have been discovered
  and resolved. Read this BEFORE writing new test code to avoid
  re-discovering these issues.
```

---

## KI-001 — Transient DI scope in WebApplicationFactory

**Symptom:** `InvalidOperationException: Cannot resolve scoped service from root provider`

**Context:** `WebApplicationFactory<Program>` creates an in-memory test host.
Services registered as `Scoped` in `Program.cs` are scoped per HTTP request.
If you try to resolve a scoped service from the root `IServiceProvider` (i.e., outside
`using var scope = services.CreateScope()`), .NET throws because the root provider
does not honor the Scoped lifetime.

**Pattern that fails:**
```csharp
// WRONG — resolves from root provider
var db = factory.Services.GetRequiredService<IDbSession>();
```

**Correct pattern:**
```csharp
// CORRECT — creates a scope, then resolves within it
using var scope = factory.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<IDbSession>();
```

**Rule:** All service resolution in HTTP integration tests must go through
`factory.Services.CreateScope()`. Never call `factory.Services.GetRequiredService<T>()`
for Scoped services.

---

## KI-002 — OnInitializedAsync prerender skips auth

**Symptom:** Blazor page appears to always see unauthenticated user in
`OnInitializedAsync`. Login is clearly valid, but handler returns `UNAUTHORIZED`.

**Root cause:** Blazor Server prerenders components on the server before the
WebSocket circuit is established. During prerender, JavaScript interop is
unavailable — `localStorage` cannot be read — so `GreenAiAuthenticationStateProvider`
always returns `Unauthenticated` during prerender.

**Consequence:** Any auth check in `OnInitializedAsync` always fails, even for
logged-in users.

**Correct pattern:** Use `OnAfterRenderAsync(firstRender: true)` for all auth
checks in Blazor pages. `OnAfterRenderAsync` only fires in the interactive circuit
where `localStorage` IS readable.

```csharp
// CORRECT — runs in circuit, after localStorage is readable
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
    // ... mediator calls
}
```

**See:** `docs/SSOT/backend/patterns/blazor-page-pattern.md` (APR_004)

---

## KI-003 — Respawn delete-order for FK-constrained tables

**Symptom:** `Respawner.ResetAsync()` fails with FK violation when resetting
the database between tests.

**Root cause:** Respawn determines deletion order by reading FK metadata. If a
table that should be deleted first has an FK to a table in `TablesToIgnore`,
Respawn may attempt to delete rows in the wrong order.

**Current `TablesToIgnore` (see `DatabaseFixture.cs`):**
```csharp
TablesToIgnore = [
    new Respawn.Graph.Table("dbo", "SchemaVersions"),  // DbUp migration history
    new Respawn.Graph.Table("dbo", "UserRoles"),        // Static system roles
    new Respawn.Graph.Table("dbo", "ProfileRoles"),     // Static system roles
    new Respawn.Graph.Table("dbo", "Languages"),        // Reference data
    new Respawn.Graph.Table("dbo", "Countries"),        // Reference data
]
```

**Rule:** If a new static/reference table is added to the schema, add it to
`TablesToIgnore` in `DatabaseFixture.cs`. Labels table is excluded through
Respawn's FK graph resolution because it references `Languages` (ignored table)
but Respawn handles this correctly by deleting `Labels` before attempting `Languages`.

**If a new FK violation appears:** Add the static table to `TablesToIgnore`.
Never add data tables to `TablesToIgnore` — only reference/static tables.

---

## KI-004 — DapperPlusSetup.Initialize() required before BulkInsert

**Symptom:** `InvalidOperationException` or license exception when calling
`.BulkInsert()`, `.BulkUpdate()`, or `.BulkMerge()` via Z.Dapper.Plus.

**Root cause:** Z.Dapper.Plus requires license initialization before any bulk
operation. In production this is handled by `DapperPlusSetup.Initialize()` in
`Program.cs`. In test contexts where the test bypasses `Program.cs` (e.g., calling
a repository directly without going through `WebApplicationFactory`), this initialization
may not have run.

**Solution:** When using `WebApplicationFactory<Program>`, initialization runs
automatically because Program.cs starts up. If writing isolated unit tests that
exercise code paths calling BulkInsert directly, call:
```csharp
DapperPlusSetup.Initialize(); // Call once — idempotent, safe to repeat
```

**Source:** `src/GreenAi.Api/Database/DapperPlusSetup.cs`

---

## KI-005 — JWT claims must match WebApplicationFactory configuration

**Symptom:** HTTP integration tests return 401 for authenticated endpoints even
when sending a valid JWT.

**Root cause:** The JWT validation parameters (Issuer, Audience, SecretKey) in the
test-started `WebApplicationFactory` must match the JWT token you generate in tests.

**Rule:** When generating JWT tokens for http_integration tests, use the same
values as `appsettings.Development.json`:
- `SecretKey`: `"dev-secret-key-min-32-chars-long!!"`
- `Issuer`: `"greenai-dev"`
- `Audience`: `"greenai-dev"`

The `GreenAiWebApplicationFactory` sets `ASPNETCORE_ENVIRONMENT=Development` to
ensure it picks up these values. The `TestJwtHelper` in
`tests/GreenAi.Tests/Http/TestJwtHelper.cs` encapsulates token generation.

**See:** `tests/GreenAi.Tests/Http/GreenAiWebApplicationFactory.cs`

---

## KI-006 — UseStatusCodePagesWithReExecute converts 401/405 to 400 in test host

**Symptom:** HTTP integration tests that send requests with no Bearer token (expecting
`401 Unauthorized`) or use wrong HTTP methods (expecting `405 Method Not Allowed`) receive
`400 BadRequest` instead.

**Root cause:** `UseStatusCodePagesWithReExecute("/not-found")` in `Program.cs` intercepts
error responses with empty bodies. When no JWT token is present and an endpoint uses
`.RequireAuthorization()`, the JWT Bearer middleware emits a `401` challenge with an empty
body. The middleware intercepts this, re-executes the request as GET `/not-found`, and
Blazor tries to prerender the NotFound page. In the in-memory `WebApplicationFactory`
test host, Blazor prerendering throws an exception (interactive circuit infrastructure
is not available), which results in a `400 BadRequest` response.

The same problem occurs for `405 Method Not Allowed` (e.g., POST to a GET-only endpoint)
whose response body is also empty.

**Correct fix:** `GreenAiWebApplicationFactory` sets `Testing:SkipStatusCodePages=true`
via `ConfigureAppConfiguration`. `Program.cs` checks this flag and skips the middleware:

```csharp
// In Program.cs — already implemented:
if (!app.Configuration.GetValue<bool>("Testing:SkipStatusCodePages"))
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
```

```csharp
// In GreenAiWebApplicationFactory — already implemented:
builder.ConfigureAppConfiguration(config =>
    config.AddInMemoryCollection(
        new Dictionary<string, string?> { ["Testing:SkipStatusCodePages"] = "true" }));
```

**Production impact:** Zero — `Testing:SkipStatusCodePages` is never set in production.

**See:** `tests/GreenAi.Tests/Http/GreenAiWebApplicationFactory.cs`,
          `src/GreenAi.Api/Program.cs`
