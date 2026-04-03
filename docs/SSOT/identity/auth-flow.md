# auth-flow

```yaml
id: auth_flow
type: flow
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/identity/auth-flow.md
red_thread: auth_flow → docs/SSOT/governance/RED_THREAD_REGISTRY.md

rules:
  MUST:
    - Login endpoint returns one of: Success | NeedsCustomerSelection | NeedsProfileSelection
    - SelectCustomer returns one of: Success | NeedsProfileSelection
    - SelectProfile returns: Success (final JWT)
    - All LoginStatus variants MUST be handled explicitly in LoginPage.razor
    - ProfileId(0) MUST NEVER appear in any issued JWT
    - Single-membership accounts auto-resolve Customer step
    - Single-profile accounts auto-resolve Profile step
  MUST_NOT:
    - Issue JWT with ProfileId value of 0
    - Skip NeedsProfileSelection handling in UI
    - Return CustomerId or ProfileId claims before they are resolved

flow:

  - step: login
    endpoint: POST /auth/login
    handler: LoginHandler
    action:
      1: validate credentials (INVALID_CREDENTIALS → 401 if fail)
      2: check account locked (ACCOUNT_LOCKED → 403 if locked)
      3: fetch UserCustomerMemberships
      4: count=0 → ACCOUNT_HAS_NO_TENANT (403)
      5: count>1 → return LoginResponse.RequiresCustomerSelection()  [status=NeedsCustomerSelection]
      6: count=1 → auto-select, fetch profiles for (UserId, CustomerId)
      7: profiles=0 → PROFILE_NOT_FOUND (404)
      8: profiles=1 → auto-select, issue final JWT
      9: profiles>1 → return LoginResponse.RequiresProfileSelection()  [status=NeedsProfileSelection]
    outputs:
      - status: Success        → token field populated, UserId+CustomerId+ProfileId+LanguageId in JWT
      - status: NeedsCustomerSelection → memberships list returned, token null
      - status: NeedsProfileSelection  → profiles list returned, token null

  - step: select_customer
    endpoint: POST /auth/select-customer
    handler: SelectCustomerHandler
    precondition: caller carries valid JWT(UserId only)
    action:
      1: validate CustomerId is in caller's memberships
      2: MEMBERSHIP_NOT_FOUND (403) if not found
      3: fetch profiles for (UserId, CustomerId)
      4: count=1 → auto-select → issue final JWT
      5: count>1 → return NeedsProfileSelection with profiles list
    outputs:
      - status: Success        → full JWT
      - status: NeedsProfileSelection → profiles list

  - step: select_profile
    endpoint: POST /auth/select-profile
    handler: SelectProfileHandler
    precondition: caller carries valid JWT(UserId+CustomerId)
    action:
      1: validate ProfileId is accessible to (UserId, CustomerId) via ProfileUserMappings
      2: PROFILE_ACCESS_DENIED (403) if not accessible
      3: issue final JWT
    output:
      - full JWT: UserId + CustomerId + ProfileId + LanguageId + roles

contracts:

  - name: LoginResponse
    shape:
      Status: LoginStatus   # enum: Success | NeedsCustomerSelection | NeedsProfileSelection
      Token: string?        # null unless Status=Success
      Memberships: List?    # populated when NeedsCustomerSelection
      Profiles: List?       # populated when NeedsProfileSelection

  - name: JWT_claims
    shape:
      sub:          UserId (int)
      customer_id:  CustomerId (int)   # absent in partial tokens
      profile_id:   ProfileId (int)    # absent in partial tokens
      lang_id:      LanguageId (int)
      role:         UserRole names
    claim_constants: src/GreenAi.Api/SharedKernel/Auth/GreenAiClaims.cs

  - name: LoginStatus_handling
    ui_contract:
      Success:                    store token + navigate to /
      NeedsCustomerSelection:     navigate to /select-customer
      NeedsProfileSelection:      navigate to /select-profile
      MUST_NOT_fall_through:      every status MUST have explicit branch — no default ignore

anti_patterns:

  - detect: LoginPage falls through NeedsProfileSelection without navigation
    why_wrong: user lands on /login with empty JWT — all auth silently fails
    fix: add explicit NeedsProfileSelection branch in Login switch/if chain

  - detect: ProfileId(0) issued in JWT
    why_wrong: violates ICurrentUser contract — ProfileId.Value must be > 0 in operational context
    fix: never call JwtTokenService.IssueToken with ProfileId(0)

  - detect: select_profile step skipped when profiles.Count == 1 during login
    why_wrong: correct — auto-selection IS allowed when count==1. This is NOT an anti-pattern.
    note: auto-selection bypasses the UI step but still runs the profile resolution logic

enforcement:

  - where: Features/Auth/Login/LoginHandler.cs
    how: ensure all ProfileResolutionResult branches return appropriate LoginResponse status

  - where: Features/Auth/Login/LoginPage.razor
    how: switch/if on LoginResponse.Status — ALL enum values handled explicitly

  - where: JwtTokenService.IssueToken
    how: assert ProfileId.Value > 0 before signing token
```
