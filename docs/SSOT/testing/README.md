# Testing — SSOT

> Authoritative patterns for all tests: unit, integration (DB), xUnit v3.

**Last Updated:** 2026-04-03

---

## Quick Navigation

| File | Topic |
|------|-------|
| [testing-strategy.md](testing-strategy.md) | Layer priority: http_integration PRIMARY, unit optional, e2e critical flows |
| [test-automation-rules.md](test-automation-rules.md) | When to write which tests, trigger table, naming, debt registry |
| [test-execution-protocol.md](test-execution-protocol.md) | Commands, execution order, failure diagnosis, pre-commit gate |
| [patterns/unit-test-pattern.md](patterns/unit-test-pattern.md) | xUnit v3 + NSubstitute, golden sample: LoginHandlerTests |
| [patterns/http-integration-test-pattern.md](patterns/http-integration-test-pattern.md) | DatabaseFixture + Respawn, golden sample: LoginRepositoryTests |
| [patterns/db-integration-pattern.md](patterns/db-integration-pattern.md) | DatabaseFixture + Respawn, real DB tests |
| [patterns/e2e-test-pattern.md](patterns/e2e-test-pattern.md) | E2ETestBase, WaitOrFailAsync, FailAsync, LoginAsync, seed fixture |
| [guides/test-running.md](guides/test-running.md) | `dotnet test --filter` commands |
| [debug-protocol.md](debug-protocol.md) | **Debug-rød-tråd**: Ping-pong, fix-layer-lock, DB log queries, E2E screenshots |

---

## Tech Stack

| Tool | Role |
|------|------|
| xUnit v3 | Test runner |
| NSubstitute | Mocking |
| DatabaseFixture | Shared DB connection (collection fixture) |
| Respawn | Reset DB between tests (seed data preserved) |

---

## File Location Convention

```
tests/GreenAi.Tests/
  Features/[Domain]/
    [Feature]HandlerTests.cs         ← unit tests (mocked deps)
    [Feature]RepositoryTests.cs      ← integration tests (real DB)
  SharedKernel/
    [Topic]Tests.cs
  DatabaseFixture.cs
```

---

## Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:
```
Handle_ValidCommand_ReturnsSuccess
Handle_MissingCustomerId_ReturnsError
GetAsync_KeyNotFound_ReturnsKeyAsValue
```

---

## Unit Test Pattern

```csharp
public class FooHandlerTests
{
    private readonly IFooRepository _repo = Substitute.For<IFooRepository>();
    private readonly ICurrentUser _user   = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command  = new FooCommand(/* ... */);
        var handler  = new FooHandler(_repo, _user);
        _repo.GetAsync(Arg.Any<int>()).Returns(new Foo { Id = 1 });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
```

---

## DatabaseFixture Pattern

Tests that need a real DB inherit from `IClassFixture<DatabaseFixture>`:

```csharp
[Collection("Database")]
public class FooRepositoryTests(DatabaseFixture db)
{
    [Fact]
    public async Task GetAsync_ExistingRow_ReturnsRow()
    {
        using var conn = db.GetConnection();
        var repo = new FooRepository(conn);
        // ...
    }
}
```

`DatabaseFixture.TablesToIgnore` — add seed-data tables so Respawn doesn't clear them.

---

## Rules

```
✅ Use Assert.* (standard xUnit) — no FluentAssertions
✅ NSubstitute Substitute.For<T>() — no Moq
✅ DatabaseFixture for DB tests (never SQLite in-memory)
✅ Deterministic test data — no random IDs
❌ No Task.Delay in tests
❌ No external network calls in unit tests
❌ No shared mutable state between tests
```

---

## Run Commands

```powershell
# All tests
dotnet test tests/GreenAi.Tests -v q

# One domain
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~Localization" -v q

# One class
dotnet test tests/GreenAi.Tests --filter "FullyQualifiedName~LocalizationServiceTests" -v n
```

---

**Last Updated:** 2026-04-02
