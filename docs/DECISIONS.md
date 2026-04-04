# Beslutningslog

## [2026-04-02] Identity & Access Foundation — COMPLETE

**Type:** FOUNDATION_GATE (regression closeout)

**Status:** FOUNDATION_COMPLETE

**Foundation invariants confirmed (Step 14 audit):**
- `ICurrentUser` is the only runtime auth context contract — no parallel interfaces
- `ProfileId > 0` enforced at pipeline level by `RequireProfileBehavior` for all `IRequireProfile`-marked requests
- No JWT issued with `ProfileId = 0` from new application code (V008 DEFAULT 0 only covers pre-Step-11 migration rows)
- Single ProfileId source: `ICurrentUser.ProfileId` from JWT claim via `HttpContextCurrentUser`
- No `Users.CustomerId` in runtime logic — column dropped (V007), `FindUserByEmail.sql` is pure identity
- Zero COALESCE/DefaultProfileId bypass patterns in production `.cs` or `.sql` files
- All auth SQL tenant-scoped (`WHERE CustomerId = @CustomerId`)

**Downstream domains UNBLOCKED:** Languages, Countries, Labels (localization)

**Status:** COMPLETE — Languages (V010), Countries (V011), Labels (V011) all implemented. Localization services, repository, ILocalizationContext, BatchUpsertLabels endpoint all done.

**Portal Core COMPLETE (2026-04-04):** 305/305 tests. Gate unlocked for Phase 3 business domains.

**Next:** Phase 3 — forretningsdomæner (SMS, messaging, delivery, lookup). Kræver Phase 3 plan.

---

## [2026-04-01] Profile is CORE domain concept — ProfileId enforcement mandatory

**Type:** ARCHITECTURE_DECISION (from analysis-tool, confidence: 0.91)

**Decision:** Profile is a first-class, co-equal data partition and capability boundary alongside Customer. ProfileId MUST remain in JWT and ICurrentUser. `profileId == 0` is a security gap that MUST be closed before any protected operation is allowed.

**Evidence from analysis-tool (analysis-tool/domains/Profile/):**
- `SmsGroups.ProfileId NOT NULL FK` — Profile directly owns all outbound messages
- 63 `ProfileRoleNames` gate all capability per profile (not per user)
- `GetAddressRestrictionForProfile(0)` returns `NoAddressRestriction` — bypasses all geographic access control (RULE_P007)
- Two WebWorkContext implementations diverge on ProfileId source (DB vs JWT claim) — must be unified (CONTRADICTION_002)
- `Users.CurrentProfileId DEFAULT 0` with no FK — 0 is a degenerate state that bypasses all restrictions (CONTRADICTION_001)

**Binding rules:**
- `ProfileId` MUST remain in `ICurrentUser` and JWT — it is NOT optional (already implemented)
- `ProfileId == 0` is INVALID for any protected runtime operation
- Profile resolution (SelectProfile) MUST precede any business operation
- Profile enforcement (Steps 11–14) MUST be complete before localization, countries, or labels are implemented
- Localization block condition EXTENDED: adds "Profile enforcement complete" alongside existing identity refactor preconditions

**What changes in governance:**
- `ai-governance/12_DECISION_REGISTRY.json`: `PROFILE_CORE_DOMAIN` added; `PROFILEID_UNRESOLVED` updated with security classification
- `ai-governance/00_SYSTEM_RULES.json`: `profile_model` section added; `build_order` rebased with Profile hardening (Steps 11–14) before localization
- `docs/IDENTITY_REFACTOR_PLAN.md`: Phase 2 — Profile Hardening added (Steps 11–15)
- `docs/CODE-REVIEW.md`: "What is Not Yet Implemented" and violations updated

**Source:** `analysis-tool/domains/Profile/000_meta.json`, `070_rules.json`

---

## [2026-03-30] DbUp over EF migrationer

**Beslutning:** DbUp med embedded `.sql`-filer, kører ved app-start  
**Begrundelse:** Ingen ORM. SQL-scripts er versionerede, reviewable og deterministiske.  
**Konvention:** `Database/Migrations/V001_*.sql`, `V002_*.sql` osv. Numbered prefix styrer rækkefølgen.  
**DB:** `(localdb)\MSSQLLocalDB` / `greenai_dev` i development.

---

## [2026-03-30] Initial scaffolding

**Beslutning:** Vertical slice architecture med co-located Blazor pages  
**Begrundelse:** AI context-vindue passer til én slice ad gangen. Alt hvad der hører til en feature er i én mappe.  
**Alternativer overvejet:** Layered (Controller/Service/Repository) — fravalgt fordi AI kræver 4+ filer i kontekst per ændring.

---

## [2026-03-30] Dapper over EF Core

**Beslutning:** Kun Dapper, ingen EF Core  
**Begrundelse:** SQL er eksplicit, versionerbar og AI-læsbar. EF's LINQ-til-SQL er uforudsigelig for AI-genereret kode.  
**Risiko:** Ingen change tracking, ingen global query filters → tenant WHERE-klausul skal altid skrives eksplicit.  
**Mitigering:** `IDbSession` wrapper + én `.sql`-fil per operation.

---

## [2026-03-30] Custom JWT over ASP.NET Identity

**Beslutning:** Custom JWT via `JwtTokenService` (ikke skabt endnu)  
**Begrundelse:** ASP.NET Identity forvirrer AI-prompts. Alt auth skal være eksplicit og traceable.  
**Konvention:** `ICurrentUser` er den eneste måde at tilgå bruger-identity i handlers og components.

---

## [2026-03-30] Strongly-typed IDs

**Beslutning:** `UserId`, `CustomerId`, `ProfileId` som separate record structs  
**Begrundelse:** AI-genereret kode blander int-IDs. Compiler-fejl er bedre end runtime-fejl.

---

## [2026-03-30] Result<T> over exceptions

**Beslutning:** Alle handlers returnerer `Result<T>` — kaster aldrig for business-fejl  
**Begrundelse:** Eksplicit kontrakt. AI kan se alle mulige udfald uden at læse exception-dokumentation.

---

<!-- Tilføj nye beslutninger øverst under en ny dato-header -->

## [2026-03-31] LanguageId placement — UserCustomerMembership (Option A)

**Type:** ARCHITECT_RESPONSE (confidence: 0.99)

**Beslutning:** `LanguageId` placeres på `UserCustomerMembership`, ikke på `Profile`.

**Arkitekt-begrundelse:** Language must be resolvable at customer-selection time without additional ambiguity or joins requiring profile selection. `UserCustomerMembership` provides a single, deterministic row per (UserId, CustomerId). This aligns with JWT requirements, simplifies queries, and maintains AI-friendly patterns.

**Regler (mandatory, fra arkitekt):**
- `LanguageId` MUST be stored on `UserCustomerMembership`
- `UserCustomerMembership` MUST include `LanguageId` in its initial creation migration
- `LanguageId` MUST be available at login-time customer resolution
- JWT MUST include `LanguageId` together with `CustomerId`
- `ICurrentUser` MUST expose `LanguageId`
- `LanguageId` MUST NOT be stored on `Profile`
- No additional query is allowed to resolve `LanguageId` after customer selection

**Constraints (mandatory, fra arkitekt):**
- Do NOT create a second migration just to add `LanguageId`
- Do NOT introduce profile-based language logic
- Do NOT defer `LanguageId` population beyond membership creation
- Default `LanguageId` must be deterministic: `1 = Danish bootstrap`

**Implementeringsvejledning (fra arkitekt):**
- Migration: `V004_UserCustomerMembership.sql` — inkluderer `LanguageId INT NOT NULL DEFAULT 1`
- FK `UserCustomerMembership.LanguageId → Languages(Id)` deferés til localization-sprint (Languages tabel eksisterer ikke endnu)
- `GetUserMemberships.sql` — inkluderer `LanguageId` i SELECT
- `UserMembershipRecord` — inkluderer `LanguageId`-felt
- `JwtTokenService.CreateToken(...)` — tilføjer `LanguageId` claim
- `ICurrentUser` + `HttpContextCurrentUser` — eksponerer `LanguageId`
- Både `LoginHandler` (auto-select path) og `SelectCustomerHandler` propagerer `LanguageId` ind i token

**Alternativer overvejet:**
- Option B: `Profiles.LanguageId` — fravalgt. Profiles ikke 1:1, kræver profile-selection-logik der ikke eksisterer endnu.

**VIOLATION-003 resolves:** `ICurrentUser.LanguageId` tilføjes som del af Step 6-implementeringen.

---

## [2026-03-31] PRIORITY_DECISION — Identity refactor before localization

**Type:** PRIORITY_DECISION  
**Decision:** `implement_now: identity_refactor` / `defer_until_after: languages, countries, labels`

**Rationale (three dependency chains):**

1. **LanguageId has no home.** Localization requires `ICurrentUser.LanguageId`. Where `LanguageId` lives (on `UserCustomerMembership` vs `Profile`) cannot be decided before the identity model is structurally finalized. Implementing localization now forces a guess that must be refactored.

2. **CustomerId is not correctly resolved after auth.** `FindUserByEmail.sql` still returns `u.CustomerId` from the `Users` table. `LoginHandler` still reads `user.CustomerId` directly. Any localization using `ICurrentUser.CustomerId` operates on a structurally incorrect identity context.

3. **Countries are customer-scoped.** A Customer belongs to a Country. Customer context is only correctly resolved after `UserCustomerMembership` exists. Countries implemented now would have undefined coupling.

**Build order enforced:**
1. Complete identity refactor (membership model + post-auth resolution + JWT + LanguageId decision)
2. Then: Languages
3. Then: Countries
4. Then: Labels (localization)

**Tracked violations at time of decision:**
- `VIOLATION-001`: `FindUserByEmail.sql` returns `u.CustomerId` from `Users` table — violates `pre_auth_sql_tenant_exception` (pre-auth queries must NOT resolve CustomerId from Users directly; CustomerId must come from `UserCustomerMembership` after auth)
- `VIOLATION-002`: `LoginHandler.cs` reads `user.CustomerId` directly — will be invalid once `Users.CustomerId` is removed
- `VIOLATION-003`: `ICurrentUser` has no `LanguageId` — blocks localization feature development

**Plan:** See `docs/IDENTITY_REFACTOR_PLAN.md`

## [2026-03-31] Global user identity med multi-tenant memberships (V003)

**Beslutning:** User er en global identitet. En User kan tilhøre flere Customers via `UserCustomerMembership`-tabellen. `Users`-tabellen må IKKE have direkte `CustomerId` FK.

**Begrundelse:** Email er globalt unik (UIX_Users_Email — ingen CustomerId-partition). Pre-auth login identificerer brugeren globalt med email/password. Tenant-kontekst bestemmes EFTER autentificering via membership-opslag.

**Login-flow (ny model):**
1. Bruger autentificeres med email + password (global identitet)
2. System henter brugerens Customer-memberships via `UserCustomerMembership`
3. Én membership → auto-select, udsteder JWT med CustomerId
4. Flere memberships → returner liste, kræver eksplicit valg fra client
5. JWT indeholder altid det valgte CustomerId som aktiv kontekst

**Alternativer overvejet:**
- Option A: Global email-uniqueness permanent, single-tenant binding → fravalgt (lukker multi-tenant)
- Option B: Per-user allowed with documentation → fravalgt (ikke arkitektonisk korrekt)
- Option C (valgt): Global identity + explicit membership resolution (confidence 0.98, arkitekt)

**Konsekvenser:**
- `Users.CustomerId` kolonne skal fjernes i næste migration
- Ny tabel: `UserCustomerMembership (UserId, CustomerId, Role, ...)`
- `LoginHandler` skal opdateres til membership-opslag
- `LoginResponse` ændres (returnerer customer-liste hvis multiple)
- Ny command/handler for eksplicit customer-valg (hvis multiple memberships)
- `LoginUserRecord` skal ikke længere indeholde `CustomerId`

**Governance-filer opdateret:**
- `00_SYSTEM_RULES.json#identity_model` — ny sektion med regler
- `05_EXECUTION_RULES.json#pre_auth_sql_tenant_exception` — præciseret til "identity resolution only"
- `05_EXECUTION_RULES.json#post_auth_tenant_resolution` — ny regel tilføjet

**Implementation-fase:** Separat sprint. Se AUDIT_RESPONSE_LOG.md internal-20260401-001.
