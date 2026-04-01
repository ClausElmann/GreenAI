# Architecture Guide — green-ai

## Architecture Style

Vertical Slice Architecture. Each feature is a self-contained folder. A reviewer or AI agent can understand a complete feature by reading one folder.

## Folder Structure

```
src/GreenAi.Api/
  Features/
    [Domain]/
      [Feature]/
        [Feature]Command.cs       ← input: record : IRequest<Result<T>>
        [Feature]Handler.cs       ← ALL logic lives here
        [Feature]Validator.cs     ← AbstractValidator<TCommand>
        [Feature]Response.cs      ← output record
        [Feature]Endpoint.cs      ← Minimal API route registration
        [Feature]Page.razor       ← Blazor page (co-located)
        [Feature].sql             ← ONE .sql file per DB operation
  SharedKernel/
    Auth/     → ICurrentUser, JwtTokenService, PasswordHasher, GreenAiClaims
    Db/       → IDbSession, DbSession, SqlLoader
    Ids/      → UserId, CustomerId, ProfileId (strongly-typed)
    Results/  → Result<T>, Error
    Tenant/   → ITenantContext, CurrentUserTenantContext
    Pipeline/ → LoggingBehavior, AuthorizationBehavior, ValidationBehavior
    Logging/  → SerilogColumnOptions, LoggingCircuitHandler
tests/GreenAi.Tests/
  Features/[Domain]/
    [Feature]HandlerTests.cs   ← unit tests (mocked)
    [Feature]RepositoryTests.cs ← integration tests (real DB)
  Database/
    SchemaIntegrationTests.cs
```

## Data Access

- Dapper only. No EF Core, no ORM, no LINQ-to-SQL.
- All SQL is in `.sql` files, loaded as embedded resources via `SqlLoader`.
- One `.sql` file per operation — no shared multi-purpose SQL files.
- Bulk operations: Z.Dapper.Plus (licensed). Use only when single-row Dapper is insufficient.

## Authentication

- Custom JWT via `JwtTokenService`. Never ASP.NET Identity.
- `ICurrentUser` is the only allowed way to access user identity inside handlers and Blazor components.
- Never inject `HttpContext` outside of middleware or `HttpContextCurrentUser`.
- Refresh token rotation: revoke old token before issuing new one.

## Multi-Tenancy

- All tenant-owned tables carry `CustomerId`.
- Every SQL query on a tenant table MUST include `WHERE CustomerId = @CustomerId`.
- No global filters. No automatic rewriting. Explicit only.
- Missing `CustomerId` in a query = blocker — stop and report.

## Profile Model *(2026-04-01 — analysis-tool confidence 0.91)*

- Profile is a **first-class, co-equal data partition alongside Customer** — NOT optional or secondary.
- `ProfileId` MUST remain in `ICurrentUser` and JWT. Already implemented via `GreenAiClaims.ProfileId` and `HttpContextCurrentUser.ProfileId`. Do NOT remove it.
- `ProfileId(0)` is a **security gap**: any code path that allows `profileId == 0` to reach business logic, filtering, or data access is a hard blocker.
- `LoginHandler` currently issues `new ProfileId(0)` as a short-lived placeholder — this is a tracked violation (VIOLATION-005). It MUST be resolved in Step 11.
- `IRequireProfile` marker will enforce `ProfileId > 0` at the pipeline level (Step 12). Mark all business commands that operate on profile-scoped data.
- Profile resolution (`SelectProfile`) MUST precede any operation that reads `ProfileId` for authorization or data access.
- All profile lookup SQL MUST include `WHERE CustomerId = @CustomerId` (tenant isolation applies to `Profiles` table).

## MediatR Pipeline Order

```
Request → LoggingBehavior → AuthorizationBehavior → ValidationBehavior → Handler
```

- `IRequireAuthentication` marker interface triggers `AuthorizationBehavior`.
- `ValidationBehavior` runs FluentValidation and returns `Result<T>.Fail` on invalid input.

## Result Pattern

- All handlers return `Result<T>`. Never throw for business logic failures.
- `Result<T>.Ok(value)` for success.
- `Result<T>.Fail(code, message)` for business errors.
- Infrastructure exceptions (DB unreachable etc.) may propagate as exceptions.

## Strongly-Typed IDs

- `UserId`, `CustomerId`, `ProfileId` are separate `record struct` types.
- Never pass raw `int` IDs across feature boundaries.

## Migrations

- DbUp with embedded `.sql` files in `Database/Migrations/`.
- Naming: `V001_Description.sql`, `V002_Description.sql` — numeric prefix controls order.
- Runs at application start via `DatabaseMigrator.Run(...)`.
- Never modify an applied migration — always add a new version.

## Logging

- Serilog structured logging to console and SQL Server `Logs` table.
- Schema for `Logs` is versioned in DbUp migrations.
- HTTP requests: `UseSerilogRequestLogging`.
- Blazor circuit events: `LoggingCircuitHandler`.
- Client-side JS errors: `/api/client-log` endpoint.

## Testing

- xUnit v3. NSubstitute for mocking. Respawn for DB reset between tests.
- `IAsyncLifetime` uses `ValueTask` (xUnit v3 requirement).
- Pass `TestContext.Current.CancellationToken` to all async calls (xUnit1051).
- Unit tests: mock all dependencies, test handler logic only.
- Integration tests: real LocalDB (`GreenAi_Test`), real SQL, Respawn resets data.
- Zero warnings in test projects.

## Build Order — Enforced Sequencing

**PRIORITY_DECISION 2026-03-31:** Identity refactor must be completed before any localization feature.

Strict sequence:
1. `UserCustomerMembership` table + migration
2. Remove `Users.CustomerId` FK + migration
3. Update `FindUserByEmail.sql` — omit `CustomerId` (pre-auth; resolves global identity only)
4. Update `LoginHandler` — post-auth membership resolution (auto-select or return list)
5. Update JWT — include resolved `CustomerId` from membership
6. Decide `LanguageId` placement (`UserCustomerMembership` or `Profile`) — add to `ICurrentUser`
7. Languages (reference table + service)
8. Countries (reference table + service)
9. Labels (localization domain)

**Hard rule:** Do not start step N+1 before step N is complete, compiled, and tests are green.  
**Hard rule:** No localization code may be written before step 6 is complete.  
**Reference:** `docs/IDENTITY_REFACTOR_PLAN.md`, `ai-governance/05_EXECUTION_RULES.json#localization_deferred_until_identity_complete`
