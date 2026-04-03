# unit-test-pattern

> **Canonical:** This is the SSOT for how to write handler unit tests in GreenAi.

```yaml
id: unit_test_pattern
type: pattern
layer: unit
version: 1.0.0
last_updated: 2026-04-03

purpose: >
  Test pure handler business logic in isolation.
  All dependencies are replaced with NSubstitute mocks.
  No database, no HTTP, no filesystem.
  Use ONLY when handler has non-trivial branching logic.

tool: NSubstitute (mock), xUnit v3 (assertion)
when_to_use:
  - handler has 3+ logical branches
  - handler coordinates multiple services
  - edge case is expensive to reproduce via DB
when_not_to_use:
  - testing SQL correctness (use http_integration instead)
  - testing endpoint routing or HTTP status (use http_integration)
  - simple CRUD with no branching (http_integration sufficient)
```

## File Location

```
tests/GreenAi.Tests/Features/[Domain]/[Feature]HandlerTests.cs
```

## Class Template

```csharp
namespace GreenAi.Tests.Features.[Domain];

public sealed class [Feature]HandlerTests
{
    // Arrange — mock all handler dependencies
    private readonly I[Feature]Repository _repository = Substitute.For<I[Feature]Repository>();
    // add other mocks as needed (e.g. IJwtTokenWriter, ICurrentUser)

    private [Feature]Handler CreateHandler() =>
        new(_repository /*, other mocks */);

    [Fact]
    public async Task Handle_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var command = new [Feature]Command(/* valid parameters */);
        _repository.[MethodName](Arg.Any<...>(), Arg.Any<CancellationToken>())
            .Returns(/* expected return value */);

        // Act
        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Handle_RepositoryReturnsNull_ReturnsFailure()
    {
        // Arrange
        _repository.[MethodName](Arg.Any<...>(), Arg.Any<CancellationToken>())
            .Returns((ReturnType?)null);

        // Act
        var result = await CreateHandler().Handle(
            new [Feature]Command(/* parameters */),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.[Expected], result.Error!.Code);
    }
}
```

## NSubstitute Rules

```yaml
substitute_setup:
  create: I[X] dep = Substitute.For<I[X]>()
  return_value: dep.Method(Arg.Any<T>()).Returns(value)
  return_null: dep.Method(Arg.Any<T>()).Returns((T?)null)
  return_task: dep.Method(Arg.Any<T>()).Returns(Task.FromResult(value))
  void_call: dep.Method(Arg.Any<T>()) does nothing (no setup needed)

arg_matchers:
  any_of_type: Arg.Any<T>()
  specific_value: Arg.Is<T>(x => x.Id == expectedId)

verification:
  called_once: await dep.Received(1).Method(Arg.Any<T>())
  not_called: await dep.DidNotReceive().Method(Arg.Any<T>())
  call_count: dep.ReceivedWithAnyArgs(n).Method(default)
```

## Golden Sample — LoginHandlerTests

The canonical reference implementation is:
```
tests/GreenAi.Tests/Features/Auth/LoginHandlerTests.cs
```

Key patterns extracted:

```csharp
// ✅ Use strongly-typed Id NEVER 0 for ProfileId
var profile = new ProfileResult(new ProfileId(42), "Test Profile", new CustomerId(1));

// ✅ CancellationToken from TestContext — not CancellationToken.None
var result = await handler.Handle(command, TestContext.Current.CancellationToken);

// ✅ Assert on error codes, not error messages
Assert.Equal(ErrorCodes.LOGIN_LOCKED, result.Error.Code);

// ✅ ProfileId(0) is FORBIDDEN — never assert or return zero-profile token
// BAD:  Assert.Equal(new ProfileId(0), result.Value.ProfileId)
// GOOD: Assert.Equal(new ProfileId(42), result.Value.ProfileId)
```

## Anti-patterns

```yaml
anti_patterns:

  - detect: calling _db.ResetAsync() in a unit test
    why_wrong: unit tests have no DB — that method doesn't exist in this context
    fix: remove DB fixture, use mocks only

  - detect: ProfileId(0) in test assertions or setup
    why_wrong: violates RED_THREAD: ProfileId(0) is never valid in a token
    fix: use ProfileId(42) or any positive integer

  - detect: new CancellationToken() or CancellationToken.None in Act
    why_wrong: misses test cancellation signal from xUnit runner
    fix: use TestContext.Current.CancellationToken

  - detect: Substitute.For<ConcreteClass>() (not interface)
    why_wrong: only works on virtual members, fragile
    fix: always substitute interfaces (I[X])

  - detect: asserting on error message strings
    why_wrong: messages can change; couples test to copy
    fix: assert on ErrorCodes.[ERROR_CODE] (strongly-typed constant)
```
