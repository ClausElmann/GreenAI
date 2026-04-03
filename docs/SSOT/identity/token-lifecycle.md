# Token Lifecycle

```yaml
id: token_lifecycle
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/identity/token-lifecycle.md
code_source:
  - src/GreenAi.Api/Features/Auth/RefreshToken/RefreshTokenHandler.cs
  - src/GreenAi.Api/Features/Auth/Login/LoginHandler.cs
  - src/GreenAi.Api/Features/Auth/SelectCustomer/SelectCustomerHandler.cs
  - src/GreenAi.Api/Features/Auth/SelectProfile/SelectProfileHandler.cs
  - src/GreenAi.Api/SharedKernel/Auth/JwtTokenService.cs
```

---

## Token Types

| Token       | Lifetime     | Storage          | Purpose                         |
|-------------|-------------|-------------------|---------------------------------|
| Access JWT  | 60 minutes  | Client memory    | Identifies caller in every request |
| Refresh token | 30 days   | DB + client      | Issues new access+refresh pair  |

Configuration: `appsettings.json > Jwt` section.

---

## JWT Payload Claims

```csharp
ClaimTypes.NameIdentifier  → UserId.Value    (int)
GreenAiClaims.CustomerId   → CustomerId.Value (int)   // "greenai:customer_id"
GreenAiClaims.ProfileId    → ProfileId.Value  (int)   // "greenai:profile_id"
GreenAiClaims.LanguageId   → languageId       (int)   // "greenai:language_id"
ClaimTypes.Email           → user.Email       (string)
```

**ProfileId semantics:**
- `ProfileId = 0` → pre-profile-selection state. Access/refresh tokens still work.
  Requests marked `IRequireProfile` return `PROFILE_NOT_SELECTED` (401).
- `ProfileId > 0` → fully resolved. All endpoints accessible per AuthorizationBehavior.

**INVARIANT: `ProfileId(0)` is NEVER issued by Login/SelectCustomer/SelectProfile in normal flow.**
The only path that produces a ProfileId=0 token is RefreshToken rotation when the
stored refresh token itself carries ProfileId=0 (a migration artefact from V008).

---

## 3-Step Auth Flow

```
Step 1: POST /api/auth/login
   → Credentials validated
   → 1 customer + 1 profile → JWT(UserId + CustomerId + ProfileId + LanguageId)
   → 1 customer + N profiles → NeedsProfileSelection (no JWT)
   → N customers → NeedsCustomerSelection (no JWT, UserId only in token with CustomerId=0)

Step 2: POST /api/auth/select-customer  [if Step 1 returns NeedsCustomerSelection]
   → Requires Bearer JWT (UserId)
   → 1 profile → JWT(UserId + CustomerId + ProfileId + LanguageId)
   → N profiles → NeedsProfileSelection

Step 3: POST /api/auth/select-profile  [if Step 1/2 returns NeedsProfileSelection]
   → Requires Bearer JWT (UserId + CustomerId)
   → Profile must be in user's accessible profiles for the customer
   → JWT(UserId + CustomerId + ProfileId + LanguageId) issued
```

---

## Refresh Token Lifecycle

**Creation:** `RefreshTokenWriter.SaveAsync()` — called by all auth steps that issue a JWT.

```csharp
// Stored in UserRefreshTokens table
Token      string          // cryptographically random (RandomNumberGenerator.GetBytes)
UserId     FK Users
CustomerId FK Customers
ProfileId  int             // carried from the session state at time of issue
ExpiresAt  DateTimeOffset  // UtcNow + 30 days
UsedAt     DateTimeOffset? // null = valid, non-null = revoked/consumed
LanguageId int
```

**Rotation (single-use):**
```
POST /api/auth/refresh
1. FindValidTokenAsync(token) → null if expired OR UsedAt != null
2. Atomic transaction:
   a. RevokeTokenAsync(record.Id)     → SET UsedAt = UtcNow
   b. SaveAsync(new token, ExpiresAt = UtcNow + 30d)
3. Issue new JWT (reads UserId, CustomerId, ProfileId, LanguageId from DB record)
4. Return new AccessToken + new RefreshToken
```

**Revocation:** Refresh tokens are single-use. After one use, `UsedAt` is set. The old
token is permanently invalid. There is no "token family" reuse detection — a stolen
refresh token that is used before the real user can use it will cause the real user's
next refresh to fail (INVALID_REFRESH_TOKEN → 401 → re-login required).

---

## Guards

| Guard | Mechanism |
|-------|-----------|
| Access token expiry | `ValidateLifetime = true, ClockSkew = TimeSpan.Zero` |
| Refresh token expiry | `ExpiresAt < UtcNow` check in `FindValidTokenAsync` |
| Refresh reuse | `UsedAt IS NULL` check in `FindValidTokenAsync` |
| ProfileId = 0 guard | `RequireProfileBehavior` returns `PROFILE_NOT_SELECTED` (401) |
| No auth guard | `AuthorizationBehavior` returns `UNAUTHORIZED` (401) |

---

## Adding New Auth Features

Rules when modifying auth handlers:
1. **Never** call `jwt.CreateToken(...)` with `ProfileId(0)` unless the step explicitly leaves profile unresolved.
2. **Always** use `_db.ExecuteInTransactionAsync` when rotating refresh tokens (atomic revoke + save).
3. **Always** read `ProfileId` from the stored DB record when refreshing — never from the incoming request.
4. **Never** skip `RequireProfileBehavior` for business endpoints — mark with `IRequireProfile`.

See also: `docs/SSOT/identity/auth-flow.md`
