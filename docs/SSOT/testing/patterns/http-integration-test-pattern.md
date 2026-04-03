# http-integration-test-pattern

> **Canonical:** This is the SSOT for how to write repository/integration tests in GreenAi.

```yaml
id: http_integration_test_pattern
type: pattern
layer: http_integration
version: 1.0.0
last_updated: 2026-04-03

purpose: >
  Test the full vertical slice from repository call to SQL result.
  Uses a real LocalDB database (GreenAI_DEV) with Respawn reset per test.
  These are the PRIMARY correctness proof for all features.

infrastructure:
  test_class: xUnit v3
  collection: "[Collection(DatabaseCollection.Name)]"
  fixture: DatabaseFixture (shared — runs DbUp once per test session)
  lifetime: IAsyncLifetime (InitializeAsync / DisposeAsync)
  seed: TestDataBuilder (inline Dapper — test project ONLY, never SqlLoader)
  db_reset: db.ResetAsync() MUST be called in InitializeAsync before every test
  cancellation: TestContext.Current.CancellationToken on all async calls
```

## File Location

```
tests/GreenAi.Tests/Features/[Domain]/[Feature]RepositoryTests.cs
tests/GreenAi.Tests/Features/[Domain]/[Feature]HandlerTests.cs  (if handler also tested)
```

## Class Template

```csharp
using GreenAi.Tests.Database;
using GreenAi.Tests.Features.Auth;  // adjust namespace to domain TestDataBuilder

namespace GreenAi.Tests.Features.[Domain];

[Collection(DatabaseCollection.Name)]
public sealed class [Feature]RepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly [Domain]TestDataBuilder _builder;

    public [Feature]RepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new [Domain]TestDataBuilder(db.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();  // ← MANDATORY
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private [Feature]Repository CreateRepository() =>
        new([Domain]TestDbSession.Create(_db.ConnectionString));

    // --- tests below ---
}
```

## Test Structure — Minimum Required Tests

```yaml
required_tests:
  happy_path:
    name_pattern: "[MethodName]_ValidInput_Returns[ExpectedResult]"
    verify: result.IsSuccess == true, result.Value != null

  not_found:
    name_pattern: "[MethodName]_UnknownId_ReturnsNullOrNotFound"
    verify: result is null OR result.IsFailure with correct error_code

  invalid_input:
    name_pattern: "[MethodName_or_Validator]_MissingRequiredField_ReturnsBadRequest"
    verify: validator fires, result.IsFailure == true
    tool: test via FluentValidation validator directly OR handler

  unauthorized:
    name_pattern: "[MethodName]_NoAuth_Returns[ErrorCode]"
    applies_when: endpoint has RequireAuthorization
    verify: endpoint returns 401

  edge_cases:
    name_pattern: "[MethodName]_[EdgeCondition]_[ExpectedBehaviour]"
    examples:
      - LockedOut account → LOGIN_LOCKED error_code
      - Inactive user → null / LOGIN_INVALID
      - Multi-membership → correct profile list returned
```

## Golden Sample — LoginRepositoryTests

The canonical reference implementation is:
```
tests/GreenAi.Tests/Features/Auth/LoginRepositoryTests.cs
tests/GreenAi.Tests/Features/Auth/AuthTestDataBuilder.cs
```

Key patterns extracted:

```csharp
// ✅ Seed a complete user with profile mapping
var userId = await _builder.InsertUserAsync();
var customerId = await _builder.InsertCustomerAsync();
var profileId = await _builder.InsertProfileAsync(customerId, "Test Profile");
await _builder.MapUserToProfileAsync(userId, profileId);

// ✅ Arrange — real repository, real connection
var repo = CreateRepository();

// ✅ Act — pass TestContext cancellation token
var result = await repo.GetLoginInfoAsync(email, CancellationToken.None);

// ✅ Assert — verify shape (IsSuccess + value fields)
Assert.NotNull(result);
Assert.Equal(userId, result.UserId);
```

## Test Data Builder Rules

```yaml
test_data_builder:
  location: tests/GreenAi.Tests/Features/[Domain]/[Domain]TestDataBuilder.cs
  uses: Dapper inline SQL (NOT SqlLoader — SqlLoader is API project only)
  connection: _db.ConnectionString (passed from fixture)

  naming_convention:
    insert_entity: InsertUserAsync(opts?) → UserId
    insert_related: InsertCustomerAsync(name?) → CustomerId
    map_join: MapUserToProfileAsync(userId, profileId)

  option_records:
    purpose: allow overriding defaults for negative tests
    example: |
      public record InsertUserOptions(
          string Email = "test@test.com",
          bool IsActive = true,
          bool IsLockedOut = false
      );

  never:
    - use SqlLoader (embedded resources from API project)
    - use hard-coded integer IDs
    - share seeded data between tests (seed fresh per test method)
```

## DatabaseFixture Reference

```csharp
// Always call ResetAsync() in InitializeAsync — THIS IS NOT OPTIONAL
public ValueTask InitializeAsync() => _db.ResetAsync();

// Tables that are NOT reset (reference/seed data preserved across tests):
// SchemaVersions, UserRoles, ProfileRoles, Languages, Countries
```

## Anti-patterns

```yaml
anti_patterns:

  - detect: not calling _db.ResetAsync() before test
    why_wrong: leftover data from previous test causes false positives or negatives
    fix: always call in InitializeAsync, never skip

  - detect: using static/hard-coded IDs in asserts
    why_wrong: fails after Respawn reset resets identity counters
    fix: capture IDs returned by TestDataBuilder insert methods

  - detect: testing with raw SQL in test methods (not via TestDataBuilder)
    why_wrong: duplication — test patterns should be reused
    fix: add method to TestDataBuilder, reference from test

  - detect: not using TestContext.Current.CancellationToken
    why_wrong: tests can hang if not properly cancelled
    fix: pass token on all async calls (not CancellationToken.None)

  - detect: creating a separate test database
    why_wrong: GreenAI_DEV IS the test DB — Respawn provides isolation
    fix: use DatabaseFixture as-is, seed per test method
```
