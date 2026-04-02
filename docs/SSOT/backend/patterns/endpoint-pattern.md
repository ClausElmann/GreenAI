# Endpoint Pattern — green-ai

> Minimal API endpoint registration pattern.

**Last Updated:** 2026-04-02

---

## File: `[Feature]Endpoint.cs`

```csharp
namespace GreenAi.Api.Features.Customer.CreateCustomer;

public static class CreateCustomerEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/customers", async (
            CreateCustomerCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/customers/{result.Value.Id}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("Customers");
    }
}
```

## Registration in Program.cs

```csharp
// Program.cs
CreateCustomerEndpoint.Map(app);
```

---

## Rules

```
✅ static class + static void Map(WebApplication app)
✅ return Result.IsSuccess → 200/201, else BadRequest(result.Error)
✅ CancellationToken passed to mediator.Send
✅ .RequireAuthorization() on all protected endpoints
✅ .WithTags("Domain") for Swagger grouping
❌ No logic in endpoint — delegate to handler via MediatR
❌ No direct DB access in endpoint
```

---

## HTTP Method → Result Mapping

| Operation | Method | Success | Failure |
|-----------|--------|---------|---------|
| Create | POST | 201 Created | 400 BadRequest |
| Read | GET | 200 OK | 404 NotFound |
| Update | PUT | 200 OK | 400/404 |
| Delete | DELETE | 204 NoContent | 404 NotFound |

---

**Last Updated:** 2026-04-02
