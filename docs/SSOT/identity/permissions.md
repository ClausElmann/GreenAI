# permissions

```yaml
id: permissions
type: pattern
ssot_source: docs/SSOT/identity/permissions.md
red_threads: [auth_flow]
applies_to: ["SharedKernel/Permissions/*.cs"]
enforcement: UserRoleNames.* and ProfileRoleNames.* constants — never magic strings
```

> **Canonical:** SSOT for GreenAi permission system — IPermissionService, UserRoles, ProfileRoles.
> **Code sources:**
> - `src/GreenAi.Api/SharedKernel/Permissions/IPermissionService.cs`
> - `src/GreenAi.Api/SharedKernel/Permissions/PermissionService.cs`
> - `src/GreenAi.Api/SharedKernel/Permissions/UserRoleNames.cs`
> - `src/GreenAi.Api/SharedKernel/Permissions/ProfileRoleNames.cs`

```yaml
id: permissions
type: pattern
version: 1.0.0
created: 2026-04-03
last_updated: 2026-04-03
ssot_source: docs/SSOT/identity/permissions.md
red_threads: [auth_flow, current_user]
related:
  - docs/SSOT/identity/current-user.md
  - docs/SSOT/backend/patterns/pipeline-behaviors.md
```

---

## Two Independent Permission Systems

```yaml
user_roles:
  table: UserRoleMappings → UserRoles
  scope: GLOBAL — no CustomerId, no tenant context
  purpose: Admin UI access flags (who can manage users, profiles, settings)
  primary_check: IPermissionService.DoesUserHaveRoleAsync(userId, roleName)
  superadmin_bypass: IPermissionService.IsUserSuperAdminAsync(userId)
  constants: UserRoleNames.cs (grow-as-needed, currently 6 defined)
  known_issue: >
    UserRoles are global by design (source system architecture).
    FOUNDATIONAL_DOMAIN_ANALYSIS documents CONTRADICTION_003 — migration to
    per-customer user roles deferred (Option D). Current implementation is correct.

profile_roles:
  table: ProfileRoleMappings → ProfileRoles
  scope: Per-profile — checks against ProfileId from ICurrentUser.ProfileId
  purpose: Operational capability flags (can this profile send SMS, use e-Boks, etc.)
  primary_check: IPermissionService.DoesProfileHaveRoleAsync(profileId, roleName)
  constants: ProfileRoleNames.cs (grow-as-needed, currently 6 defined)
  note: This is the PRIMARY feature gate for all send/operational features
```

---

## IPermissionService Interface

```csharp
// Inject: IPermissionService _permissions
// Registered: AddScoped<IPermissionService, PermissionService>() in Program.cs

// Check global admin role
bool hasRole = await _permissions.DoesUserHaveRoleAsync(user.UserId, UserRoleNames.ManageUsers);

// Check SuperAdmin bypass
bool isSuperAdmin = await _permissions.IsUserSuperAdminAsync(user.UserId);

// Check profile capability
bool canSendSms = await _permissions.DoesProfileHaveRoleAsync(user.ProfileId, ProfileRoleNames.SmsConversations);
```

---

## Role Constants

### UserRoleNames (global admin roles)

```csharp
UserRoleNames.SuperAdmin              // bypasses most authorization checks
UserRoleNames.API                     // API access token generation
UserRoleNames.ManageUsers             // customer admin: manage users
UserRoleNames.ManageProfiles          // customer admin: manage profiles
UserRoleNames.CustomerSetup           // customer admin: setup customer settings
UserRoleNames.TwoFactorAuthenticate   // 2FA requirement
```

### ProfileRoleNames (operational capability flags)

```csharp
ProfileRoleNames.HaveNoSendRestrictions      // override send restrictions
ProfileRoleNames.CanSendByEboks              // e-Boks channel capability
ProfileRoleNames.CanSendByVoice              // voice channel capability
ProfileRoleNames.UseMunicipalityPolList      // municipality police list access
ProfileRoleNames.CanSendToCriticalAddresses  // critical address send permission
ProfileRoleNames.SmsConversations            // SMS conversation feature
```

> **Rule:** Do not add speculative role names. Add to `UserRoleNames.cs` or `ProfileRoleNames.cs`
> only when the handler that checks them is being implemented.

---

## Handler Usage Pattern

```csharp
// ✅ CORRECT — handler with permission gate
public sealed class DoSensitiveActionHandler(IPermissionService permissions, ICurrentUser user)
    : IRequestHandler<DoSensitiveActionCommand, Result<SomeResponse>>
{
    public async Task<Result<SomeResponse>> Handle(DoSensitiveActionCommand cmd, CancellationToken ct)
    {
        // 1. Pipeline already enforced IRequireAuthentication → user.UserId is valid
        // 2. Check profile capability
        var canAct = await permissions.DoesProfileHaveRoleAsync(user.ProfileId, ProfileRoleNames.SmsConversations);
        if (!canAct)
            return Result<SomeResponse>.Fail("FORBIDDEN", "Profile lacks required capability.");

        // ... rest of handler
    }
}
```

---

## Where NOT to Check Permissions

```yaml
do_not_check_in:
  - endpoints (Endpoint.cs): only call mediator.Send, never check permissions
  - validators (Validator.cs): validators check input shape, not authorization
  - pipeline behaviors (except AuthorizationBehavior): behaviors are generic,
    not feature-aware

check_in:
  - handlers (Handler.cs): feature-specific permission gating
  - OR: add IRequireProfile marker + let RequireProfileBehavior enforce a base profile check,
    then handler checks specific capability
```

---

## SQL Layer

```sql
-- DoesUserHaveRole.sql — checks UserRoleMappings (global, no CustomerId)
SELECT CAST(CASE WHEN EXISTS (
    SELECT 1 FROM UserRoleMappings urm
    JOIN UserRoles ur ON ur.Id = urm.UserRoleId
    WHERE urm.UserId = @UserId AND ur.Name = @RoleName
) THEN 1 ELSE 0 END AS BIT) AS HasRole;

-- DoesProfileHaveRole.sql — checks ProfileRoleMappings (per-profile)
SELECT CAST(CASE WHEN EXISTS (
    SELECT 1 FROM ProfileRoleMappings prm
    JOIN ProfileRoles pr ON pr.Id = prm.ProfileRoleId
    WHERE prm.ProfileId = @ProfileId AND pr.Name = @RoleName
) THEN 1 ELSE 0 END AS BIT) AS HasRole;
```

---

## Anti-patterns

```yaml
- detect: permission check in Endpoint.cs before mediator.Send
  why_wrong: endpoint is not the authorization layer — breaks separation of concerns
  fix: move check to handler

- detect: hardcoded role name string in handler ("SuperAdmin" instead of UserRoleNames.SuperAdmin)
  why_wrong: typo-prone; not discoverable via IDE navigation
  fix: always use UserRoleNames.* or ProfileRoleNames.* constants

- detect: checking UserRole for an operational feature (e.g. SmsConversations)
  why_wrong: SmsConversations is a PROFILE capability, not a USER admin role
  fix: use DoesProfileHaveRoleAsync + ProfileRoleNames constants

- detect: new role name not added to UserRoleNames.cs / ProfileRoleNames.cs
  why_wrong: undiscoverable magic string; typo produces silent false
  fix: add constant to the relevant class before writing the handler
```
