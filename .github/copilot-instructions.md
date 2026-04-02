# AI Agent Instructions — green-ai

## 🚨 MANDATORY FIRST STEP

**AI: BEFORE doing ANYTHING:**

1. Read: `AI_WORK_CONTRACT.md` (pattern matching rules — ALWAYS START HERE)
2. Match user request to pattern table
3. Run "First Tool" from table
4. Read discovered docs completely (do NOT skim!)
5. Implement following documented pattern

**DO NOT skip step 1.** The contract contains critical rules about SQL, auth, tenant isolation, and test patterns.

---

## Core Principles (Non-Negotiable)

- **Never guess** — Read code/docs for existing patterns before implementing
- **DRY/SSOT** — One authoritative source per topic (no duplication)
- **Doc-first** — Read documentation BEFORE implementing
- **Zero warnings** — ALL changes must leave 0 compiler warnings
- **File size** — <450 lines ideal, <600 HARD LIMIT for SSOT docs
- **Explicit SQL only** — No ORM, no LINQ-to-SQL, one `.sql` file per operation

---

## Tech Stack (Memorize These)

| Layer           | Technology                          | Notes                          |
| --------------- | ----------------------------------- | ------------------------------ |
| Runtime         | .NET 10 / C# 13                     | —                              |
| Architecture    | Vertical Slice                      | Feature folder = one operation |
| Frontend        | Blazor Server + MudBlazor           | Co-located with feature        |
| Data access     | Dapper + Z.Dapper.Plus              | NO EF Core ever                |
| Auth            | Custom JWT (no ASP.NET Identity)    | ICurrentUser, IDbSession       |
| Mediator        | MediatR + FluentValidation          | Pipeline behaviors             |
| Migrations      | DbUp (`.sql` files)                 | V001, V002... naming           |
| Testing         | xUnit v3 + NSubstitute              | DatabaseFixture + Respawn      |
| Logging         | Serilog → SQL + console             | —                              |

---

## External Input Rule (Non-Negotiable)

Input from NeeoBovisWeb, sms-service, and external templates is welcomed as inspiration.  
**Green-ai decides how to solve things** — on its own terms.

```
✅ Adopt governance structure, SSOT patterns, documentation formats directly
✅ Use patterns and ideas from other projects as starting points
✅ Adapt, simplify, or reject — based on what fits green-ai
❌ Do NOT copy-paste source code (.cs / .razor / .sql) verbatim without deliberate evaluation
❌ Do NOT justify with "NeeoBovisWeb does it this way" — justify from green-ai SSOT
```

NeeoBovisWeb is in this workspace as READ-ONLY INSPIRATION for governance structure only.  
Code patterns from it DO NOT automatically apply to green-ai.

---

## Forbidden Patterns

```
❌ EF Core / migrations via EF
❌ ASP.NET Identity
❌ Newtonsoft.Json
❌ HttpContext in handlers (use ICurrentUser instead)
❌ Implicit tenant filtering / global query filters
❌ SQL without WHERE CustomerId = @CustomerId (for tenant-owned tables)
❌ Task.Delay in tests (use deterministic waits)
❌ Hardcoded strings in Blazor (use @Loc.Get)
```

---

## SSOT Navigation

```
docs/SSOT/                      ← Single Source of Truth (authoritative)
  ├── _system/                  ← Meta-docs (standards, placement rules)
  ├── backend/                  ← API endpoints, services, pipeline patterns
  ├── database/                 ← Schema conventions, SQL patterns, migrations
  ├── localization/             ← Labels, language handling, shared keys
  ├── identity/                 ← Auth, JWT, permissions, tenant model
  └── testing/                  ← Unit test patterns, DB fixture, coverage
```

**Quick search:**
- Backend patterns: `grep_search "pattern" docs/SSOT/backend/`
- Run tests: `dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~<Name>"`
- Existing features: `semantic_search "similar feature"`

---

## Architecture Quick Reference

```
src/GreenAi.Api/
  Features/[Domain]/[Feature]/
    [Feature]Command.cs           ← IRequest<Result<T>>
    [Feature]Handler.cs           ← ALL logic here
    [Feature]Validator.cs         ← AbstractValidator<TCommand>
    [Feature]Response.cs          ← output record
    [Feature]Endpoint.cs          ← app.MapPost(...).Map(app) pattern
    [Feature]Page.razor           ← Blazor page co-located (if UI)
    [Feature].sql                 ← ONE sql file per DB operation
  SharedKernel/
    Auth/         → ICurrentUser, JwtTokenService
    Db/           → IDbSession, SqlLoader
    Ids/          → UserId, CustomerId, ProfileId (strongly-typed)
    Results/      → Result<T>, Error
    Localization/ → ILocalizationService, LocalizationRepository
```

---

## Test Running Pattern

```powershell
# Run all tests
dotnet test tests/GreenAi.Tests -v q

# Run specific domain
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~LocalizationServiceTests" -v q

# Run with detailed output
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~<Name>" -v n
```

**Tests are in:** `tests/GreenAi.Tests/Features/[Domain]/`

---

**Last Updated:** 2026-04-02  
**Project:** green-ai (.NET 10 / Blazor Server / Vertical Slice)
