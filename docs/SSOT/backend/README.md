# Backend — SSOT

> Authoritative patterns for all backend code: endpoints, handlers, validators, pipeline.

**Last Updated:** 2026-04-02

---

## Quick Navigation

### Patterns (code patterns with examples)

| File | Topic |
|------|-------|
| [endpoint-pattern.md](patterns/endpoint-pattern.md) | Minimal API endpoint registration (`Map(app)`) |
| [handler-pattern.md](patterns/handler-pattern.md) | MediatR handler + Result<T> |
| [result-pattern.md](patterns/result-pattern.md) | Result<T>, Error, error code catalog, ToHttpResult() |
| [blazor-page-pattern.md](patterns/blazor-page-pattern.md) | OnAfterRenderAsync + PrincipalHolder + StateHasChanged |
| [validator-pattern.md](patterns/validator-pattern.md) | FluentValidation + pipeline |
| [sql-pattern.md](patterns/sql-pattern.md) | SqlLoader + embedded `.sql` files |

### Architecture

| File | Topic |
|------|-------|
| [vertical-slice.md](architecture/vertical-slice.md) | Feature folder structure, one operation per folder |
| [pipeline-behaviors.md](patterns/pipeline-behaviors.md) | Logging, Auth, Validation, RequireProfile |

---

## Core Rules

```
✅ IRequest<Result<T>>     — all commands/queries
✅ ONE .sql file per operation
✅ SqlLoader for embedded SQL (no inline strings)
✅ ICurrentUser for identity context (never HttpContext)
✅ IDbSession for DB connection
✅ Validators registered via MediatR pipeline (no manual call)
❌ No EF Core
❌ No ASP.NET Identity
❌ No inline SQL strings
```

---

## Feature Folder Structure

```
Features/[Domain]/[Feature]/
  [Feature]Command.cs       ← record : IRequest<Result<T>>
  [Feature]Handler.cs       ← IRequestHandler implementation
  [Feature]Validator.cs     ← AbstractValidator<TCommand>
  [Feature]Response.cs      ← output record
  [Feature]Endpoint.cs      ← app.MapPost / MapGet / Map(app)
  [Feature]Page.razor       ← Blazor page (if UI feature)
  [Feature].sql             ← ONE sql file per DB operation
```

**Enforcement:** `scripts/governance/Validate-GreenAiCompliance.ps1`

---

**Last Updated:** 2026-04-02
