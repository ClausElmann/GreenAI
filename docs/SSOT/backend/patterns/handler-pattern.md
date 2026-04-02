# Handler Pattern — green-ai

> MediatR handler structure with Result<T> return.

**Last Updated:** 2026-04-02

---

## Command (Input)

```csharp
// [Feature]Command.cs
namespace GreenAi.Api.Features.Customer.CreateCustomer;

public record CreateCustomerCommand(string Name, string CvrNumber)
    : IRequest<Result<CreateCustomerResponse>>;
```

---

## Handler (Logic)

```csharp
// [Feature]Handler.cs
namespace GreenAi.Api.Features.Customer.CreateCustomer;

public class CreateCustomerHandler(
    IDbSession db,
    ICurrentUser user,
    SqlLoader sql) : IRequestHandler<CreateCustomerCommand, Result<CreateCustomerResponse>>
{
    public async Task<Result<CreateCustomerResponse>> Handle(
        CreateCustomerCommand command, CancellationToken ct)
    {
        // 1. Build parameters
        var parameters = new
        {
            Name      = command.Name,
            CvrNumber = command.CvrNumber,
            CreatedBy = user.UserId
        };

        // 2. Load + execute SQL
        var query  = sql.Load("Features/Customer/CreateCustomer/CreateCustomer.sql");
        var result = await db.Connection.QuerySingleAsync<CreateCustomerResponse>(query, parameters);

        return Result.Success(result);
    }
}
```

---

## Response (Output)

```csharp
// [Feature]Response.cs
namespace GreenAi.Api.Features.Customer.CreateCustomer;

public record CreateCustomerResponse(int Id, string Name, DateTimeOffset CreatedAt);
```

---

## Rules

```
✅ IRequest<Result<T>>              — always Result<T>
✅ Constructor injection (primary ctor preferred)
✅ sql.Load("path/to/Feature.sql")  — never inline SQL
✅ ICurrentUser for identity (not HttpContext)
✅ IDbSession for connection
✅ Return Result.Success(value) or Result.Failure(error)
❌ No business logic in command/response records
❌ No direct EF, no repository abstraction — handlers own SQL
```

---

**Last Updated:** 2026-04-02
