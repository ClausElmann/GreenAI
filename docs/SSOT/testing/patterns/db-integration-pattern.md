# DB Integration Test Pattern

> **Canonical:** SSOT for handler tests that use a real database (LocalDB).
> **See also:** [http-integration-test-pattern.md](http-integration-test-pattern.md) for repository-layer tests.

```yaml
id: db_integration_test_pattern
type: pattern
layer: db_integration
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/testing/patterns/db-integration-pattern.md
red_threads: []
related:
  - docs/SSOT/testing/patterns/http-integration-test-pattern.md
  - docs/SSOT/testing/patterns/unit-test-pattern.md
```

---

## When to Use This Pattern

| Pattern | Use when |
|---------|----------|
| **db-integration** (this) | Handler + real DB (no HTTP layer) — fast integration proof |
| **http-integration** | Repository + real SQL — lowest-layer correctness proof |
| **unit** | Pure logic, no DB — handler with mocked repositories |

Use db-integration when:
- You want to test a complete handler (ICurrentUser + IDbSession) against real DB
- No HTTP endpoint exists (Blazor-driven features)
- Testing tenant isolation behavior in full

---

## Infrastructure

```
Collection:  [Collection(DatabaseCollection.Name)]
Fixture:     DatabaseFixture (shared — DbUp migrations run once per session)
Lifetime:    IAsyncLifetime
DB Reset:    db.ResetAsync() in InitializeAsync — MANDATORY before every test
Builder:     [Domain]TestDataBuilder — seeds minimal valid DB state
CT:          TestContext.Current.CancellationToken
```

---

## Class Template

```csharp
using GreenAi.Api.Features.CustomerAdmin.GetProfiles;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Database;
using NSubstitute;

namespace GreenAi.Tests.Features.CustomerAdmin;

[Collection(DatabaseCollection.Name)]
public sealed class GetProfilesHandlerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly CustomerAdminTestDataBuilder _builder;

    public GetProfilesHandlerTests(DatabaseFixture db)
    {
        _db      = db;
        _builder = new CustomerAdminTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();  // ← MANDATORY
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static GetProfilesHandler CreateHandler(CustomerId customerId)
    {
        var db   = new DbSession(DatabaseFixture.ConnectionString);
        var user = Substitute.For<ICurrentUser>();
        user.CustomerId.Returns(customerId);
        user.IsAuthenticated.Returns(true);
        return new GetProfilesHandler(db, user);
    }

    [Fact]
    public async Task Handle_ProfileExists_ReturnsProfileInList()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var _          = await _builder.InsertProfileAsync(customerId, userId, "Test Profile");

        var result = await CreateHandler(customerId).Handle(
            new GetProfilesQuery(),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Test Profile", result.Value![0].Name);
    }

    [Fact]
    public async Task Handle_EmptyCustomer_ReturnsEmptyList()
    {
        var customerId = await _builder.InsertCustomerAsync();

        var result = await CreateHandler(customerId).Handle(
            new GetProfilesQuery(),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}
```

---

## Mandatory Rules

```
✅ db.ResetAsync() in InitializeAsync — clears all data per test
✅ Use DatabaseFixture.ConnectionString — never hardcode connection string
✅ Substitute ICurrentUser — return controlled CustomerId/ProfileId/UserId
✅ TestContext.Current.CancellationToken — on all async calls
✅ Test both success and not-found/empty cases
✅ Test tenant isolation: profile from other customer must NOT be visible
❌ Never use Task.Delay — xUnit analyzer will flag it
❌ Never seed data outside of TestDataBuilders
```

---

## Test File Location

```
tests/GreenAi.Tests/Features/[Domain]/[Feature]HandlerTests.cs
```

Example: `tests/GreenAi.Tests/Features/CustomerAdmin/GetProfilesHandlerTests.cs`

---

**Last Updated:** 2026-04-06
