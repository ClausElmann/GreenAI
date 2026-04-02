# Status

> Last updated: 2026-04-02 (session 2)

## Løsningsstatus

### Infrastruktur
- [x] Solution scaffoldet (`GreenAi.slnx`)
- [x] `GreenAi.Api` oprettet (Blazor Server, .NET 10)
- [x] `GreenAi.Tests` oprettet (xUnit + NSubstitute)
- [x] SharedKernel: `ICurrentUser`, `IDbSession`, `DbSession`, `SqlLoader`, `Result<T>`, `StrongIds`, `ITenantContext`, `ValidationBehavior`, `AuthorizationBehavior`, `RequireProfileBehavior`, `LoggingBehavior`
- [x] Database: DbUp + 14 migrationer (V001–V014), `DatabaseMigrator`
- [x] `GreenAi.DB` SSDT-projekt (14 tabeller)
- [x] Dapper + Z.Dapper.Plus (licenseret)
- [x] Serilog → SQL + console
- [x] `DatabaseFixture` + Respawn
- [x] Governance: `.github/copilot-instructions.md`, `AI_WORK_CONTRACT.md`, `docs/SSOT/`, `scripts/governance/`

### Identity & Access Foundation (FOUNDATION_COMPLETE)
- [x] `Users`, `Customers`, `Profiles` tabeller
- [x] `UserCustomerMemberships` — multi-tenant membership med LanguageId (renamed V013)
- [x] `ProfileUserMappings` — many-to-many profile access (V009)
- [x] `UserRoles`, `UserRoleMappings` — globale admin-roller
- [x] `ProfileRoles`, `ProfileRoleMappings` — operationelle capability-flags
- [x] `CustomerUserRoleMappings` — customer role policy-tabel
- [x] `UserRefreshTokens` med ProfileId + LanguageId (V006, V008)
- [x] Login-flow: `LoginHandler` → membership resolution → single/multi profile
- [x] `SelectCustomerHandler` — explicit customer selection, JWT med LanguageId
- [x] `SelectProfileHandler` — explicit profile selection, ProfileId > 0 guaranteed
- [x] `RefreshTokenHandler` — rotation med ProfileId + LanguageId
- [x] `IPermissionService` — `DoesUserHaveRoleAsync` + `DoesProfileHaveRoleAsync` + `IsUserSuperAdminAsync`
- [x] `RequireProfileBehavior` — pipeline enforcement af ProfileId > 0
- [x] `IRequireAuthentication` + `IRequireProfile` marker interfaces

### Localization (COMPLETE)
- [x] `Languages` tabel + seed (da/sv/en/fi/nb/de) — V010
- [x] `Iso639_1` kolonne på Languages + UIX på Countries.NumericIsoCode — V012
- [x] `Countries` tabel + seed (DK/SE/GB/FI/NO/DE) — V011
- [x] `Labels` tabel med indexes — V011
- [x] `Language.cs`, `Country.cs` entiteter
- [x] `CountryIds`, `LanguageIds`, `PhoneConstants` — strongly typed, no interchangeability
- [x] `ILocalizationRepository` + `LocalizationRepository` (Dapper)
- [x] `ILocalizationService` + `LocalizationService` (fail-open)
- [x] DI registreret i `Program.cs`
- [x] `DatabaseFixture`: Countries i TablesToIgnore
- [x] V014 — seed `shared.*` labels (DA + EN) — 30 rows
- [x] `ILocalizationContext` + `LocalizationContext` — Scoped Blazor helper (`@Loc.Get("key")`)
- [x] `BatchUpsertLabels` endpoint (`POST /api/labels/batch-upsert`) — SuperAdmin only, validator, MERGE SQL

### Public API v1 (IN PROGRESS)
- [x] `POST /api/v1/auth/token` — maskin-til-maskin login med `UserRole = API`
- [ ] Øvrige v1-endpoints (følger domæne-implementering)

### Mangler stadig
- [ ] CI/CD pipeline
- [ ] Rate limiting på auth-endpoints
- [ ] Audit logging
- [ ] SAML2 SSO
- [ ] Azure AD / Entra ID login
- [ ] Impersonation (Step 5b)
- [ ] Admin: ManageUsers, ManageProfiles, UserRoleAssignment

## Tests
**105 tests grønne** (inkl. 14 LocalizationServiceTests, 9 PermissionService-tests, 4 many-to-many tests).
