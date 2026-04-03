# Error Codes Catalog

```yaml
id: error_codes
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/conventions/error-codes.md
code_source: src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs

rule: >
  ALL error codes used in Result<T>.Fail("CODE", ...) must be registered in
  ResultExtensions.cs. The RESULT-001 compliance rule enforces this automatically.
  This document provides the human-readable catalog for feature authors.
```

---

## Canonical Error Codes

All codes are defined in `ResultExtensions.cs` switch expression.
The switch maps code → HTTP status. Any unmapped code returns HTTP 500.

### 400 — Bad Request (input rejected before business logic)

| Code | HTTP | When to use |
|------|------|-------------|
| `VALIDATION_ERROR` | 400 | FluentValidation failure (emitted automatically by `ValidationBehavior`) |

### 401 — Unauthorized (caller not authenticated or credentials wrong)

| Code | HTTP | When to use |
|------|------|-------------|
| `UNAUTHORIZED` | 401 | Request requires auth but no valid JWT present (emitted by `AuthorizationBehavior`) |
| `INVALID_CREDENTIALS` | 401 | Email/password mismatch. Use identical message for both cases (no enumeration) |
| `INVALID_REFRESH_TOKEN` | 401 | Refresh token not found, expired, or already used |
| `PROFILE_NOT_SELECTED` | 401 | Request requires `IRequireProfile` but ProfileId = 0 in JWT |

### 403 — Forbidden (authenticated but not permitted)

| Code | HTTP | When to use |
|------|------|-------------|
| `FORBIDDEN` | 403 | Authenticated user lacks permission for the operation |
| `ACCOUNT_LOCKED` | 403 | User account is locked (`IsLockedOut = true`) |
| `ACCOUNT_HAS_NO_TENANT` | 403 | Authenticated user has no customer membership |
| `MEMBERSHIP_NOT_FOUND` | 403 | User is authenticated but not a member of the requested customer |
| `PROFILE_ACCESS_DENIED` | 403 | User does not have access to the requested profile |

### 404 — Not Found

| Code | HTTP | When to use |
|------|------|-------------|
| `PROFILE_NOT_FOUND` | 404 | Profile record does not exist (not an access issue) |
| `NOT_FOUND` | 404 | Generic resource not found (use specific code if available) |

### 409 — Conflict

| Code | HTTP | When to use |
|------|------|-------------|
| `EMAIL_TAKEN` | 409 | Attempted email change to an address already registered |

### 500 — Internal Server Error (unmapped)

| Code | HTTP | When to use |
|------|------|-------------|
| `NO_CUSTOMER` | 500 | User has no customer claim — should be caught earlier in auth flow |
| *(any unmapped)* | 500 | Codes not in the switch → default 500. NEVER leave unmapped codes. |

---

## Adding a New Error Code

1. **Check this catalog first** — use an existing code if one fits.
2. If no code fits: add a new entry to the `switch` in `ResultExtensions.cs`.
3. Choose the correct HTTP status (prefer specific over generic).
4. Update this catalog with the new entry.
5. The RESULT-001 compliance rule will automatically enforce the new code in all features.

Example:
```csharp
// In ResultExtensions.cs switch:
"CUSTOMER_NOT_FOUND" => HttpResults.Problem(result.Error.Message, statusCode: 404),
```

---

## Anti-patterns

```
❌ Result<T>.Fail("SOMETHING_CUSTOM", "...")  — code not in ResultExtensions → HTTP 500
❌ Result<T>.Fail("error", "...")             — lowercase not in switch → HTTP 500
❌ Result<T>.Fail("NOT FOUND", "...")         — spaces → not in switch → HTTP 500
✅ Result<T>.Fail("NOT_FOUND", "...")         — exact match, uppercase, underscores
```

## Related Files

- `src/GreenAi.Api/SharedKernel/Results/ResultExtensions.cs` — switch implementation
- `src/GreenAi.Api/SharedKernel/Results/Result.cs` — Result<T> type definition
- `docs/SSOT/backend/patterns/result-pattern.md` — usage guide
