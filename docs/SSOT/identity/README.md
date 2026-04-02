# Identity & Auth — SSOT

> Authoritative patterns for authentication, authorisation, tenant isolation, and JWT.

**Last Updated:** 2026-04-02

---

## Quick Navigation

| File | Topic |
|------|-------|
| [auth-flow.md](architecture/auth-flow.md) | Login → customer selection → profile selection → JWT |
| [tenant-isolation.md](tenant-isolation.md) | CustomerId rules, pre-auth exceptions |
| [permissions.md](patterns/permissions.md) | IPermissionService, role checks, SuperAdmin |
| [jwt-claims.md](patterns/jwt-claims.md) | Claim names (GreenAiClaims), token content |

---

## Core Concepts

### Identity Model

```
User          ← global, email + password
Customer      ← tenant (company)
Profile       ← operational context within a customer
Membership    ← User ↔ Customer link (UserCustomerMembership)
ProfileMap    ← User ↔ Profile link (ProfileUserMappings)
```

### Auth Flow

```
POST /auth/login
  → validate credentials
  → resolve memberships
  → single membership  → auto-select customer
  → multi membership   → return list for selection

POST /auth/select-customer
  → resolve profiles for customer
  → single profile  → auto-select + issue JWT
  → multi profile   → return list for selection

POST /auth/select-profile
  → issue final JWT with CustomerId + ProfileId + LanguageId
```

### JWT Claims (GreenAiClaims constants)

| Claim | Type |
|-------|------|
| `sub` | UserId |
| `customer_id` | CustomerId |
| `profile_id` | ProfileId |
| `lang_id` | LanguageId |
| `role` | UserRole names |

---

## Tenant Isolation Rule

**MANDATORY for all SQL against tenant-owned tables:**

```sql
WHERE CustomerId = @CustomerId
```

**Pre-auth exception:** `FindUserByEmail`, `FindValidRefreshToken` — identify global user only. `CustomerId` is NOT available yet and NOT required.

**Post-auth:** All queries use `ICurrentUser.CustomerId`. Never read `CustomerId` from request body — always from JWT via `ICurrentUser`.

---

## Pipeline Behaviors (order)

1. `LoggingBehavior` — request/response logging
2. `ValidationBehavior` — FluentValidation (returns 400 on failure)
3. `AuthorizationBehavior` — checks `IRequireAuthentication`
4. `RequireProfileBehavior` — enforces `ProfileId > 0` for `IRequireProfile`

---

## Key Interfaces

```csharp
ICurrentUser        // UserId, CustomerId, ProfileId, LanguageId, IsAuthenticated
IRequireAuthentication   // marker: handler needs valid JWT
IRequireProfile          // marker: handler needs ProfileId > 0
IPermissionService  // DoesUserHaveRoleAsync, DoesProfileHaveRoleAsync, IsUserSuperAdminAsync
```

---

**Last Updated:** 2026-04-02
