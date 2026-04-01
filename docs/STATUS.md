# Status

> Last updated: 2026-04-01

## Løsningsstatus

### Infrastruktur
- [x] Solution scaffoldet (`GreenAi.slnx`)
- [x] `GreenAi.Api` oprettet (Blazor Server, .NET 10)
- [x] `GreenAi.Tests` oprettet (xUnit + NSubstitute)
- [x] SharedKernel: `ICurrentUser`, `IDbSession`, `DbSession`, `SqlLoader`, `Result<T>`, `StrongIds`, `ITenantContext`, `ValidationBehavior`, `AuthorizationBehavior`, `RequireProfileBehavior`, `LoggingBehavior`
- [x] Database: DbUp + 10 migrationer (V001–V010), `DatabaseMigrator`
- [x] `GreenAi.DB` SSDT-projekt (13 tabeller)
- [x] Dapper + Z.Dapper.Plus (licenseret)
- [x] Serilog → SQL + console
- [x] `DatabaseFixture` + Respawn

### Identity & Access Foundation (FOUNDATION_COMPLETE)
- [x] `Users`, `Customers`, `Profiles` tabeller
- [x] `UserCustomerMembership` — multi-tenant membership med LanguageId
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

### Localization
- [x] `Languages` tabel + seed (da/sv/en/fi/nb/de) — V010
- [ ] `Countries` tabel
- [ ] `Labels` tabel
- [ ] `ILocalizationService`

### Mangler stadig
- [ ] `Countries` + `Labels` (localization step 17–18)
- [ ] CI/CD pipeline
- [ ] Rate limiting på auth-endpoints
- [ ] Audit logging
- [ ] SAML2 SSO
- [ ] Azure AD / Entra ID login
- [ ] Impersonation (Step 5b)
- [ ] Admin: ManageUsers, ManageProfiles, UserRoleAssignment

## Tests
**76 tests grønne** (pre V010). Ny: ~9 PermissionService-tests (kræver V010 migration på LocalDB).
