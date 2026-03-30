# Status

## Løsningsstatus
- [x] Solution scaffoldet (`GreenAi.sln`)
- [x] `GreenAi.Api` oprettet (Blazor Server)
- [x] `GreenAi.Tests` oprettet (xUnit)
- [x] SharedKernel: `ICurrentUser`, `IDbSession`, `DbSession`, `SqlLoader`, `Result<T>`, `StrongIds`, `ITenantContext`, `ValidationBehavior`
- [x] Første feature-slice: `Features/System/Ping`
- [x] 2/2 tests grønne
- [x] Auth: `JwtTokenService`, `GreenAiClaims`, `JwtOptions`, `TokenResult`, `HttpContextCurrentUser`
- [x] Database: DbUp + `V001_Baseline.sql` (Customers, Users, Profiles, UserRefreshTokens) + `DatabaseMigrator`
- [x] LocalDB: `greenai_dev` konfigureret i `appsettings.Development.json`
- [ ] Auth: `CustomAuthenticationStateProvider` (Blazor circuit)
- [ ] Tenant: `TenantMiddleware`, `ITenantContext` implementation
- [ ] Login feature: `LoginCommand`, `LoginHandler`, `LoginPage.razor`
- [ ] CI pipeline

## Næste opgave
`CustomAuthenticationStateProvider` + login-feature slice.
