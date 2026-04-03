# endpoint-pattern

```yaml
id: endpoint_pattern
type: pattern
ssot_source: docs/SSOT/backend/patterns/endpoint-pattern.md
red_threads: [result_pattern]
applies_to: ["Features/**/Endpoint.cs"]
enforcement: Validate-GreenAiCompliance.ps1 APR-007
```

> **Canonical:** This is the SSOT for all Minimal API endpoints in GreenAi.
> **Golden sample:** `src/GreenAi.Api/Features/Auth/ChangePassword/ChangePasswordEndpoint.cs`

```yaml
id: endpoint_pattern
type: pattern
version: 2.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/endpoint-pattern.md
red_threads: [result_pattern]
```

---

## Endpoint Template

```csharp
// [Feature]Endpoint.cs
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.[Domain].[Feature];

public static class [Feature]Endpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/[domain]/[action]", async (
            [Feature]Command command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithTags("[Domain]");
    }
}
```

## GET Endpoint (query, no body)

```csharp
app.MapGet("/api/[domain]/[resource]", async (
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new [Feature]Query(), ct);
    return result.ToHttpResult();
})
.RequireAuthorization()
.WithTags("[Domain]");
```

---

## Registration in Program.cs

```csharp
[Feature]Endpoint.Map(app);
```

---

## Rules

```yaml
MUST:
  - static class + static void Map(IEndpointRouteBuilder app)
  - ALWAYS call: return result.ToHttpResult()
  - No inline status code logic — ALL HTTP mapping is in ResultExtensions.cs
  - CancellationToken passed to mediator.Send
  - .RequireAuthorization() on all protected endpoints
  - .WithTags("Domain") for Swagger grouping
  - Parameter: IEndpointRouteBuilder (not WebApplication) for testability

MUST_NOT:
  - Results.Ok(...) / Results.Created(...) / Results.BadRequest(...) inline
  - result.IsSuccess ternary in endpoint — use result.ToHttpResult() only
  - Direct DB access in endpoint
  - Business logic in endpoint — all logic in handler via MediatR
```

## HTTP Status Mapping

All status codes determined by `ResultExtensions.ToHttpResult()`.
Canonical source: `src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs`
Reference: `docs/SSOT/backend/patterns/result-pattern.md` → error_code_catalog

**Last Updated:** 2026-04-03

