# Feature Template — green-ai

Use this template when creating any new feature slice.

## Folder

```
src/GreenAi.Api/Features/[Domain]/[Feature]/
```

## Required Files

### [Feature]Command.cs
```csharp
namespace GreenAi.Api.Features.[Domain].[Feature];

public record [Feature]Command(/* input properties */) : IRequest<Result<[Feature]Response>>;
```

### [Feature]Handler.cs
```csharp
namespace GreenAi.Api.Features.[Domain].[Feature];

public sealed class [Feature]Handler : IRequestHandler<[Feature]Command, Result<[Feature]Response>>
{
    private readonly I[Feature]Repository _repository;

    public [Feature]Handler(I[Feature]Repository repository)
        => _repository = repository;

    public async Task<Result<[Feature]Response>> Handle([Feature]Command command, CancellationToken cancellationToken)
    {
        // ALL business logic lives here
        // Never call HttpContext
        // Never throw for business errors — return Result.Fail(...)
    }
}
```

### [Feature]Validator.cs
```csharp
namespace GreenAi.Api.Features.[Domain].[Feature];

public sealed class [Feature]Validator : AbstractValidator<[Feature]Command>
{
    public [Feature]Validator()
    {
        RuleFor(x => x.Property).NotEmpty();
    }
}
```

### [Feature]Response.cs
```csharp
namespace GreenAi.Api.Features.[Domain].[Feature];

public record [Feature]Response(/* output properties */);
```

### [Feature]Endpoint.cs
```csharp
namespace GreenAi.Api.Features.[Domain].[Feature];

public static class [Feature]Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/[domain]/[feature]", async (
            [Feature]Command command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}
```

> **RULE: ALL API routes MUST be defined in [Feature]Endpoint.cs — NEVER inline in Program.cs.**
> Program.cs MUST only call `[Feature]Endpoint.Map(app)` — it must not contain route logic.
> Infrastructure-only routes (e.g. client-side error ingestion) are the only allowed exception and must be explicitly labeled with a `// INFRASTRUCTURE` comment.

### [Feature].sql (one per DB operation)
```sql
-- FindSomethingByX.sql
SELECT col1, col2
FROM   TableName
WHERE  CustomerId = @CustomerId   -- MANDATORY for tenant tables
AND    col = @param
```

### [Feature]Page.razor (if UI needed)
```razor
@page "/[domain]/[feature]"
@using GreenAi.Api.Features.[Domain].[Feature]
@inject IMediator Mediator

<PageTitle>[Feature]</PageTitle>

@* Blazor UI here *@
```

## Checklist Before Submitting

- [ ] Command is a `record` implementing `IRequest<Result<T>>`
- [ ] Handler returns `Result<T>` — never throws for business errors
- [ ] Validator registered (FluentValidation picks it up via assembly scan)
- [ ] SQL uses `@param` syntax — no string interpolation
- [ ] Tenant queries include `WHERE CustomerId = @CustomerId`
- [ ] Endpoint.cs created with static `Map(IEndpointRouteBuilder app)` method
- [ ] `[Feature]Endpoint.Map(app)` called explicitly in Program.cs
- [ ] No route logic written inline in Program.cs
- [ ] Unit test covers handler logic with mocked dependencies
- [ ] Integration test covers repository SQL (if DB access added)
- [ ] Zero compiler warnings

## Rules

- Never put logic in Endpoint or Page — only in Handler
- Never use `HttpContext` in Handler — use `ICurrentUser`
- Never write inline SQL strings — always a `.sql` file
- Never share a `.sql` file between two features
- Never add cross-feature dependencies
- **Never define business routes inline in Program.cs — always in [Feature]Endpoint.cs**
