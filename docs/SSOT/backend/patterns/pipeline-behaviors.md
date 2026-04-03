# pipeline-behaviors

```yaml
id: pipeline_behaviors
type: flow
ssot_source: docs/SSOT/backend/patterns/pipeline-behaviors.md
red_threads: [result_pattern, auth_flow]
applies_to: ["SharedKernel/Pipeline/*.cs"]
enforcement: marker interface compilation — IRequireAuthentication on command/query
```

> **Canonical:** This is the SSOT for all MediatR pipeline behaviors in GreenAi.
> **Code source:** `src/GreenAi.Api/SharedKernel/Pipeline/`

```yaml
id: pipeline_behaviors
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/pipeline-behaviors.md
red_threads: [result_pattern, auth_flow, current_user]
```

---

## Pipeline Order (MANDATORY — registered in Program.cs)

```
1. LoggingBehavior          → logs request type + response (all requests)
2. AuthorizationBehavior    → rejects unauthenticated if IRequireAuthentication
3. RequireProfileBehavior   → rejects ProfileId=0 if IRequireProfile
4. ValidationBehavior       → runs AbstractValidator<TCommand> (commands only)
5. Handler                  → business logic
```

> Order is critical. Authorization runs before validation so invalid tokens never reach SQL.

---

## Marker Interfaces

### `IRequireAuthentication`
File: `src/GreenAi.Api/SharedKernel/Pipeline/AuthorizationBehavior.cs`

```csharp
// Declare on the command/query:
public sealed record MyCommand(string X)
    : IRequest<Result<MyResponse>>, IRequireAuthentication;
```

**When to apply:**
- Any command or query that requires a valid JWT (UserId resolved)
- All CustomerAdmin features, all profile-scoped features
- Auth-step commands that have partial JWT (SelectCustomer, SelectProfile): YES
- Login command (no JWT yet): NO

**On failure:** returns `Result<T>.Fail("UNAUTHORIZED", "Authentication required")` — never throws

---

### `IRequireProfile`
File: `src/GreenAi.Api/SharedKernel/Pipeline/RequireProfileBehavior.cs`

```csharp
// Declare on the command/query:
public sealed record MyCommand(string X)
    : IRequest<Result<MyResponse>>, IRequireAuthentication, IRequireProfile;
```

**When to apply:**
- Commands or queries that need a resolved ProfileId (ProfileId.Value > 0)
- Business operations on profile-scoped data
- NOT on auth commands (Login, SelectCustomer, SelectProfile) — those establish the profile

**On failure:** returns `Result<T>.Fail("PROFILE_NOT_SELECTED", "Profile not selected")` — never throws

---

## Decision Table

| Feature type | IRequireAuthentication | IRequireProfile |
|---|---|---|
| Login | NO | NO |
| SelectCustomer | YES | NO |
| SelectProfile | YES | NO |
| RefreshToken | NO | NO |
| ChangePassword | YES | NO |
| CustomerAdmin queries | YES | YES |
| Business write commands | YES | YES |
| Localization admin | YES | YES |

---

## Anti-patterns

```yaml
anti_patterns:

  - detect: manual auth check in handler body
    example: "if (!user.IsAuthenticated || HasCustomerId()) return Result.Fail(...)"
    why_wrong: >
      AuthorizationBehavior already handles this via IRequireAuthentication marker.
      Manual checks create duplicate logic that can diverge from pipeline behavior.
      HasCustomerId() try/catch pattern is symptom of missing marker interface.
    fix: Add IRequireAuthentication to command/query interface. Remove manual check.

  - detect: try/catch around user.CustomerId or user.ProfileId to check presence
    example: "try { _ = user.CustomerId; return true; } catch (InvalidOperationException) { return false; }"
    why_wrong: >
      ICurrentUser throws when accessed before authentication or profile resolution.
      This check is a workaround for a missing pipeline marker.
    fix: Use IRequireAuthentication + IRequireProfile markers. Never catch InvalidOperationException.

  - detect: IRequireProfile without IRequireAuthentication
    why_wrong: RequireProfileBehavior runs AFTER AuthorizationBehavior — presumes auth already confirmed
    fix: Always pair: IRequireProfile implies IRequireAuthentication

  - detect: command in Auth/ folder with IRequireProfile
    why_wrong: Auth commands create identity — they cannot require an already-resolved profile
    fix: Remove IRequireProfile from Login, SelectCustomer, SelectProfile commands
```

---

## Behavior Implementations (reference)

| Behavior | File | Error code on fail |
|---|---|---|
| AuthorizationBehavior | SharedKernel/Pipeline/AuthorizationBehavior.cs | UNAUTHORIZED |
| RequireProfileBehavior | SharedKernel/Pipeline/RequireProfileBehavior.cs | PROFILE_NOT_SELECTED |
| ValidationBehavior | SharedKernel/Pipeline/ValidationBehavior.cs | VALIDATION_ERROR |

**Last Updated:** 2026-04-03
