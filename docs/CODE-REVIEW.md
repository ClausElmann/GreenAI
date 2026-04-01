# GreenAi — External Code Review Package

> Prepared for external reviewer. Describes technology choices, architecture patterns, security measures, and known trade-offs.
> Last updated: 2026-04-01

---

## 1. Project Overview

GreenAi is a .NET 10 web application built with Blazor Server and a co-hosted Web API. It is a greenfield project designed from the start around AI-assisted development — meaning explicitness, traceability, and minimal "magic" are first-class design goals.

---

## 2. Technology Stack

| Concern | Choice | Rationale |
|---|---|---|
| Runtime | .NET 10 / C# 13 | Current LTS-candidate. AOT-ready, latest language features. |
| Web framework | Blazor Server + Minimal API | Single-host reduces ops complexity. Blazor handles UI; Minimal API handles REST. |
| ORM | Dapper 2.x | Explicit SQL. No hidden queries, no change tracking side effects. |
| Bulk operations | Z.Dapper.Plus | Performant bulk-insert/update where row volumes make single-row Dapper insufficient. Licensed commercially. |
| Database migrations | DbUp | SQL scripts checked in as embedded resources. Versioned, reviewable, deterministic. Runs at app start. |
| Database | SQL Server (LocalDB in dev, full SQL Server in production) | |
| Authentication | Custom JWT (not ASP.NET Identity) | Explicit token lifecycle. Independent of Identity scaffolding. |
| Dependency injection | Built-in `Microsoft.Extensions.DependencyInjection` + Scrutor (assembly scanning) | |
| CQRS/Mediator | MediatR 14 | Standard pattern for decoupling request handling from dispatch. |
| Validation | FluentValidation 12 | Declarative validation rules, wired into MediatR pipeline. |
| Logging | Serilog → SQL Server + console | Structured logging. `Logs` table schema is versioned in DbUp. |
| JSON | System.Text.Json (built-in) | No Newtonsoft dependency. CamelCase + null-omission configured globally. |
| UI component library | MudBlazor 8 | |
| Testing | xUnit v3 + NSubstitute + Respawn | See section 5. |

---

## 3. Architecture

### Strongly-typed Domain IDs

All cross-boundary domain identifiers use `readonly record struct` wrappers:

```csharp
public readonly record struct UserId(int Value);
public readonly record struct CustomerId(int Value);
public readonly record struct ProfileId(int Value);
```

Repository interfaces and handler boundaries only accept these typed IDs — not raw `int`. Internal DB record types (e.g. `LoginUserRecord`) still carry raw `int` since they are raw query projections, not domain objects.

### Vertical Slice Architecture

Each feature is a self-contained folder containing all layers:

```
Features/
  Auth/
    Login/
      LoginCommand.cs       ← input record : IRequest<Result<T>>
      LoginHandler.cs       ← IRequestHandler<TCmd, Result<T>>
      LoginValidator.cs     ← AbstractValidator<TCmd>
      LoginResponse.cs      ← output record
      LoginEndpoint.cs      ← Minimal API route mapping
      LoginPage.razor        ← Blazor page (co-located)
      FindUserByEmail.sql   ← one SQL file per DB operation
```

**Why:** A code reviewer or AI agent can understand a complete feature by reading one folder. No cross-cutting service layers to trace.

### MediatR Pipeline (ordered)

```
Request → LoggingBehavior → AuthorizationBehavior → RequireProfileBehavior → ValidationBehavior → Handler
```

- `LoggingBehavior`: traces every request/response with duration
- `AuthorizationBehavior`: rejects unauthenticated requests marked with `IRequireAuthentication`
- `RequireProfileBehavior`: blocks requests marked `IRequireProfile` when `ProfileId.Value <= 0` — returns `Result<T>.Fail("PROFILE_NOT_SELECTED")`. Apply to all business commands that read profile-scoped data.
- `ValidationBehavior`: runs FluentValidation; returns `Result<T>.Fail` on invalid input

### Result<T> pattern

All handlers return `Result<T>` — never throw for business-logic failures:

```csharp
public static Result<T> Ok(T value)
public static Result<T> Fail(string code, string message)
```

This makes all possible outcomes visible at the call site without inspecting exception documentation.

---

## 4. Security

### Password hashing

- Algorithm: PBKDF2/SHA-512 via `Rfc2898DeriveBytes.Pbkdf2`
- 100,000 iterations
- 32-byte cryptographically random salt per user (`RandomNumberGenerator.GetBytes`)
- Constant-time comparison via `CryptographicOperations.FixedTimeEquals` (prevents timing attacks)

### JWT

- Signed with HMAC-SHA256
- Issuer, audience, and lifetime validated on every request
- `ClockSkew = TimeSpan.Zero` (no tolerance window)
- Refresh token rotation: old token revoked (`UsedAt` set) before new token issued
- Refresh tokens stored in DB with `ExpiresAt` and `UsedAt` columns — single-use enforced at query level

### Tenant Isolation

- `ITenantContext` exposes `CustomerId` to all handlers
- SQL queries must include explicit `WHERE CustomerId = @CustomerId` — no implicit global filter
- Risk: relies on developer discipline. Mitigated by code review and integration tests that verify tenant boundary.

### Identity Model (multi-tenant membership)

**Architectural decision 2026-03-31:** User is a global identity, not bound to a single Customer.

- Email uniqueness is global (`UIX_Users_Email` — no `CustomerId` partition)
- A User may belong to multiple Customers via `UserCustomerMembership (UserId, CustomerId, Role, ...)`
- Pre-authentication queries identify the user globally — they do NOT resolve tenant context
- After authentication, Customer context is resolved explicitly:
  - Single membership → auto-select
  - Multiple memberships → client must explicitly select before JWT is issued
- JWT always contains the selected `CustomerId` as active context
- Silent tenant switching is forbidden

**Implementation status:** Governance encoded. Schema migration and login flow refactor are pending (next sprint).

### Input validation

- All commands validated by FluentValidation before reaching handlers
- Validation runs in MediatR pipeline — cannot be bypassed by calling handler directly

### No raw SQL string interpolation

- All SQL is parameterized via Dapper's `@param` syntax
- SQL files are embedded resources, not constructed at runtime

---

## 5. Testing Strategy

| Type | Tool | Coverage |
|---|---|---|
| Unit tests | xUnit v3 + NSubstitute | All handlers tested with mocked dependencies |
| Integration tests | xUnit v3 + Respawn + LocalDB | Repository layer tested against real SQL Server |
| Schema tests | xUnit v3 + Dapper | Verifies all required tables exist after migrations |

**Test database isolation:** Respawn resets data (not schema) between tests via `IAsyncLifetime`. `SchemaVersions` table excluded from reset to preserve DbUp state.

**Current test count:** 32 tests, all green.

---

## 6. Known Trade-offs

| Trade-off | Decision | Rationale |
|---|---|---|
| No EF Core | Dapper only | SQL is explicit and auditable. EF's generated SQL is not predictable enough for AI-assisted development. |
| No ASP.NET Identity | Custom JWT | Removes 200+ generated files. Auth contract is fully explicit. |
| No global query filters | Explicit tenant WHERE | Global filters in EF are invisible — explicit WHERE clauses are reviewable. |
| No repository pattern | Direct DB calls in handlers | One less abstraction layer. SQL is in `.sql` files, testable directly. |
| Blazor Server (not WASM) | Same-host, persistent circuit | Simpler deployment. Downside: server memory per active user. Acceptable for current scale. |
| Z.Dapper.Plus (commercial) | Required for bulk performance | No viable open-source alternative with same simplicity for SQL Server bulk ops. |

---

## 7. What is Not Yet Implemented

- **Countries / Labels (localization step 2/3)** — Languages table + seed is done (V010). Countries and Labels are next.
- CI/CD pipeline (Azure DevOps / GitHub Actions)
- Production deployment configuration
- Rate limiting on auth endpoints
- Audit logging (who changed what, when)
- Integration tests for handler layer (currently unit-tested with mocks only)
- SAML2 SSO per-customer flow
- Azure AD / Entra ID login
- Impersonation (Step 5b — VIOLATION-004 pre-implementation)
- Admin feature slices: ManageUsers, ManageProfiles, UserRoleAssignment

---

## 8. Tracked Pre-Refactor Violations

These violations are **known and accepted** as pre-refactor state. They must NOT be replicated in new code. Each must be resolved as part of the identity refactor or profile hardening sprint.

| ID | File | Violation | Rule Violated | Status |
|---|---|---|---|---|
| VIOLATION-001 | `Features/Auth/Login/FindUserByEmail.sql` | Returns `u.CustomerId` directly from `Users` table | `pre_auth_sql_tenant_exception` — pre-auth SQL must NOT resolve CustomerId from Users; CustomerId must come from `UserCustomerMembership` post-auth | **RESOLVED** (Step 8) |
| VIOLATION-002 | `Features/Auth/Login/LoginHandler.cs` | Reads `user.CustomerId` directly; passes it to `JwtTokenService` and `SaveRefreshTokenAsync` | `identity_model` — Users must NOT contain CustomerId FK after refactor | **RESOLVED** (Step 8) |
| VIOLATION-003 | `SharedKernel/Auth/ICurrentUser.cs` | `ICurrentUser` has no `LanguageId` property | Blocks localization feature; LanguageId placement must be decided during identity refactor | **RESOLVED** (Step 6) |
| VIOLATION-004 | `SharedKernel/Auth/HttpContextCurrentUser.cs` | `IsImpersonating` / `OriginalUserId` are dead code — `GreenAiClaims.ImpersonatedUserId` is never set in any issued token | Do not use `IsImpersonating` in authorization logic until Step 5b of identity refactor is implemented | Pre-implementation, tracked |
| VIOLATION-005 | `Features/Auth/Login/LoginHandler.cs` | Issues `new ProfileId(0)` as placeholder — no profile resolution | `PROFILE_CORE_DOMAIN` — profileId == 0 is a security gap | **RESOLVED** (Step 11) |
| VIOLATION-006 | `Features/Auth/SelectCustomer/FindMembership.sql` | `COALESCE(p.[Id], 0)` — SelectCustomer may issue JWT with `ProfileId(0)` | `PROFILE_CORE_DOMAIN` — ProfileId must be resolved to a real value | **RESOLVED** (Step 11) |
| VIOLATION-007 | `SharedKernel/Pipeline/AuthorizationBehavior.cs` | Does not enforce `ProfileId > 0` for operations requiring a profile context | `PROFILE_CORE_DOMAIN` — enforcement must be at pipeline level | **RESOLVED** (Step 12 — `RequireProfileBehavior` added) |

---

## 9. Identity & Access Foundation — Complete Criteria

> **Status as of 2026-04-02: FOUNDATION_COMPLETE**

The following invariants are now universally guaranteed by infrastructure. They require no handler-level enforcement.

| Invariant | Mechanism | Verified |
|---|---|---|
| `ICurrentUser` is the **only** runtime auth context contract | Single interface; no parallel auth context; `ITenantContext` is a thin delegate over `ICurrentUser.CustomerId` | Step 14 audit |
| `UserId > 0` for any authenticated request | `AuthorizationBehavior` — `IsAuthenticated` check | Step 7 |
| `CustomerId > 0` for any post-customer-selection request | JWT claim; `ICurrentUser.CustomerId` from claim; claim missing → parse exception | Step 7 |
| `ProfileId > 0` for any business operation marked `IRequireProfile` | `RequireProfileBehavior` — blocks `ProfileId.Value <= 0` with `PROFILE_NOT_SELECTED` | Step 12 |
| No JWT issued with `ProfileId = 0` from new application code | `LoginHandler`, `SelectCustomerHandler`, `SelectProfileHandler` — all produce `ProfileId > 0` or `NeedsProfileSelection` (no JWT) | Step 11 |
| Single `ProfileId` source: JWT claim via `ICurrentUser.ProfileId` | `HttpContextCurrentUser` reads `GreenAiClaims.ProfileId` claim; no DB fallback per request | Step 14 audit |
| No `Users.CustomerId` in runtime logic | Column dropped (V007 migration); `FindUserByEmail.sql` is a pure identity query | Step 8 |
| No COALESCE/DefaultProfileId bypass in SQL | All auth SQL audited; V005/V007 migration comments are the sole matches | Step 14 audit |
| All SQL returns ProfileId from real `Profiles.Id` rows | `GetAvailableProfiles.sql`, `GetProfiles.sql` return `p.[Id]` with strict `WHERE` | Step 11 |
| Tenant isolation enforced in all profile/membership SQL | `WHERE CustomerId = @CustomerId` present in all tenant-scoped queries | Per `sql_tenant_guard` rule |

**Downstream domains (localization, countries, labels) are now UNBLOCKED.**
Languages table + seed (V010) is done. Countries and Labels are next.

---

## 10. Permission System

### Two independent capability gates

| Gate | Table | Method | Scope |
|---|---|---|---|
| Admin/UI roles | `UserRoleMappings (UserId, UserRoleId)` | `IPermissionService.DoesUserHaveRoleAsync` | Global — no CustomerId |
| Operational capabilities | `ProfileRoleMappings (ProfileId, ProfileRoleId)` | `IPermissionService.DoesProfileHaveRoleAsync` | Per Profile |

**Rule:** `DoesProfileHaveRoleAsync` is the PRIMARY enforcement mechanism for all messaging/operational features. `DoesUserHaveRoleAsync` governs admin UI access only.

### UserRole seed (V010 — grow as handlers require)

`SuperAdmin`, `API`, `ManageUsers`, `ManageProfiles`, `CustomerSetup`, `TwoFactorAuthenticate`

### ProfileRole seed (V010 — grow as handlers require)

`HaveNoSendRestrictions`, `CanSendByEboks`, `CanSendByVoice`, `UseMunicipalityPolList`, `CanSendToCriticalAddresses`, `SmsConversations`

### CustomerUserRoleMappings

Policy-only table `(CustomerId, UserRoleId)` — **no UserId column**. Defines which UserRoles a customer configures in its admin UI. Not a per-user assignment. See `FOUNDATIONAL_DOMAIN_ANALYSIS` core concept `CustomerUserRoleMapping`.

### Known limitation

UserRoles are global (no CustomerId) — CONTRADICTION_003 from source system analysis acknowledged. Option D migration (customer-scoped roles) is deferred to a later phase.
