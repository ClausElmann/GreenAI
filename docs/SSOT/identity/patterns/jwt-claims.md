# JWT Claims Reference

> **Canonical:** SSOT for JWT claim names, token content, and ICurrentUser resolution.
> **Code source:** `src/GreenAi.Api/SharedKernel/Auth/GreenAiClaims.cs` + `JwtTokenService.cs`

```yaml
id: jwt_claims
type: reference
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/identity/patterns/jwt-claims.md
red_threads: [auth_flow, current_user]
related:
  - docs/SSOT/identity/auth-flow.md
  - docs/SSOT/identity/current-user.md
```

---

## Claim Constants (`GreenAiClaims`)

```csharp
// src/GreenAi.Api/SharedKernel/Auth/GreenAiClaims.cs
public static class GreenAiClaims
{
    public const string CustomerId         = "greenai:customer_id";
    public const string ProfileId          = "greenai:profile_id";
    public const string LanguageId         = "greenai:language_id";
    public const string ImpersonatedUserId = "greenai:impersonated_user_id";
}
```

> **Rule:** Always use `GreenAiClaims.X` constants — never magic strings like `"greenai:customer_id"`.

---

## Token Payload

| Claim | Standard? | Value | ICurrentUser property |
|-------|-----------|-------|----------------------|
| `sub` (NameIdentifier) | JWT standard | UserId (int) | `UserId` |
| `email` | JWT standard | User email | `Email` |
| `greenai:customer_id` | Custom | CustomerId (int) | `CustomerId` |
| `greenai:profile_id` | Custom | ProfileId (int) | `ProfileId` |
| `greenai:language_id` | Custom | LanguageId (int) | `LanguageId` |
| `iss` | JWT standard | Configured issuer | — |
| `aud` | JWT standard | Configured audience | — |
| `exp` | JWT standard | Access token expiry | — |

---

## Token Configuration (appsettings)

```json
"Jwt": {
  "Issuer": "green-ai",
  "Audience": "green-ai-client",
  "SecretKey": "...",
  "AccessTokenExpiryMinutes": 60
}
```

---

## Access Token vs Refresh Token

| | Access Token | Refresh Token |
|---|---|---|
| Format | JWT (signed) | Random 256-bit hex |
| Lifetime | `AccessTokenExpiryMinutes` (default 60 min) | 30 days |
| Storage | Client memory | `UserRefreshTokens` table |
| Contains | All identity claims | Opaque token only |
| Rotation | On every refresh | YES — single-use |

---

## Reading Claims in Code

```csharp
// Via ICurrentUser (PREFERRED — DI injected)
var userId     = user.UserId;       // UserId struct
var customerId = user.CustomerId;   // CustomerId struct
var profileId  = user.ProfileId;    // ProfileId struct
var languageId = user.LanguageId;   // int

// Via ClaimsPrincipal (only in GreenAiClaims-aware code)
var raw = principal.FindFirstValue(GreenAiClaims.CustomerId);
var customerId = new CustomerId(int.Parse(raw!));
```

> Never read `GreenAiClaims` constants directly in handlers — use `ICurrentUser`.

---

**Last Updated:** 2026-04-06
