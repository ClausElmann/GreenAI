# Vertical Slice Architecture

> **Canonical:** SSOT for feature folder structure in green-ai.
> **See also:** [backend/README.md](../README.md) for quick navigation.

```yaml
id: vertical_slice
type: architecture
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/backend/architecture/vertical-slice.md
red_threads: [result_pattern, sql_embedded]
```

---

## Principle

Each feature is a **self-contained vertical slice**: one operation = one folder with all its files.

```
Features/[Domain]/[FeatureName]/
  [Feature]Command.cs     ← or [Feature]Query.cs
  [Feature]Handler.cs     ← all business logic here
  [Feature]Validator.cs   ← only if input validation needed
  [Feature]Endpoint.cs    ← MapPost/MapGet registration
  [Feature]Response.cs    ← output record (if complex)
  [Feature].sql           ← ONE per DB operation (multiple .sql allowed)
  [Feature]Page.razor     ← Blazor page if applicable
```

---

## Domain Folders

| Domain | Path | Purpose |
|--------|------|---------|
| Auth | `Features/Auth/` | Login, token, password flows |
| CustomerAdmin | `Features/CustomerAdmin/` | Admin UI features |
| Identity | `Features/Identity/` | User identity management |
| Localization | `Features/Localization/` | Label management |
| System | `Features/System/` | Ping, health checks |
| Api (v1) | `Features/Api/V1/` | Machine-to-machine API |

---

## Rules

```
✅ One operation per folder (GetUsers ≠ GetUserDetails — separate folders)
✅ Handler is the ONLY place business logic lives
✅ Endpoint only maps HTTP → MediatR.Send → HTTP result
✅ Validator is optional — only add if there is real input validation
✅ SQL files in the SAME folder as the handler that uses them
❌ No shared folders for SQL (cross-feature SQL reuse is not the pattern)
❌ No business logic in endpoints
❌ No DB access in Blazor pages (use Mediator.Send)
```

---

## Example: Login Feature

```
Features/Auth/Login/
  LoginCommand.cs          record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>
  LoginHandler.cs          sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
  LoginValidator.cs        sealed class LoginValidator : AbstractValidator<LoginCommand>
  LoginEndpoint.cs         static class LoginEndpoint { Map(IEndpointRouteBuilder) }
  LoginResponse.cs         record LoginResponse(...)
  LoginPage.razor          @page "/login"
  FindUserByEmail.sql
  GetUserMemberships.sql
  RecordFailedLogin.sql
  ResetFailedLogin.sql
  SaveRefreshToken.sql
```

---

## Every Feature Is Registered In

`docs/SSOT/_system/feature-contract-map.json` — machine-readable contract for compliance checks.

---

**Last Updated:** 2026-04-06
