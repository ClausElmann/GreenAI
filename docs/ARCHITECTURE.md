# GreenAi — SSOT

## Stack
- .NET 10 / C# 13
- Blazor Server (same-host som Web API)
- Dapper (ingen EF Core, ingen ORM)
- SQL Server
- Custom JWT (ingen ASP.NET Identity)
- MediatR + FluentValidation + Scrutor
- xUnit + NSubstitute

## Projekter
| Projekt | Formål |
|---|---|
| `src/GreenAi.Api` | Blazor Server + Web API + SharedKernel + Features |
| `tests/GreenAi.Tests` | Unit tests (én fil per handler) |

## Mappestruktur (konvention)
```
src/GreenAi.Api/
  Features/
    [Domain]/
      [Feature]/
        [Feature]Command.cs      ← record : IRequest<Result<T>>
        [Feature]Handler.cs      ← IRequestHandler<TCmd, Result<T>>
        [Feature]Validator.cs    ← AbstractValidator<TCmd>
        [Feature]Response.cs     ← output record
        [Feature]Endpoint.cs     ← minimal API mapping
        [Feature]Page.razor      ← Blazor page (co-located)
        [Feature].sql            ← ÉN sql-fil per operation
  SharedKernel/
    Auth/    → ICurrentUser
    Db/      → IDbSession, DbSession, SqlLoader
    Ids/     → UserId, CustomerId, ProfileId
    Results/ → Result<T>, Error
    Tenant/  → ITenantContext
    Pipeline/→ ValidationBehavior (ordered)
tests/GreenAi.Tests/
  Features/[Domain]/[Feature]/
    [Feature]HandlerTests.cs
```

## Ikke-accepterede mønstre
- Inline SQL strings
- HttpContext i handlers
- EF Core (selv til migrering — brug DbUp)
- ASP.NET Identity
- Repository pattern / service layer abstraktion
- Generic base repositories
- Implicit tenant-filtrering

## Governance
AI-governance regler lever i `analysis-tool/ai-governance/` og er autoriteten for prompt-regler, anti-patterns og stack-analyse.
