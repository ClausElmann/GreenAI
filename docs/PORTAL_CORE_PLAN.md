# Portal Core Plan — green-ai

> **Phase:** 2 — Core Portal Flows
> **Gate:** SLICE-006 (Foundation) completed ✅ — all 226 tests green
> **Created:** 2026-04-04
> **Prerequisite:** `docs/FOUNDATION-PLAN.md` Phase 1 complete

---

## Formål

Transformerer systemet fra:

> **Backend platform** (stabil, testet, sikker)

til:

> **Brugbar portal-kerne** (selvbetjening, email-flows, admin, UI-fundament)

Fasen er domæne-agnostisk — ingen SMS, messaging, delivery eller integrations.

---

## Gate-regel

> Feature-domæner (messaging, sms, delivery, lookup) starter IKKE før  
> **alle Portal Core slices er grønne** og golden sample fase 2 passerer.

---

## Regler

| ID | Regel |
|----|-------|
| R-PHASE2-001 | Ingen forretningsdomæner: sms, messaging, delivery, integrations |
| R-PHASE2-002 | Alle nye endpoints: handler-pattern → `Result<T>` → `ToHttpResult()` |
| R-PHASE2-003 | Alt UI: `Loc.Get(...)`, loading state, error handling |
| R-PHASE2-004 | Alle nye features tilføjes til `feature-contract-map.json` |
| R-PHASE2-005 | Ingen hardcoded strings i Blazor (se `❌ FORBUDT`) |
| R-PHASE2-006 | Testnavngivning: `tests/Http/{Domain}/{Feature}Tests.cs` — ikke SliceXXX |

---

## Allerede eksisterer (arvet fra Foundation)

| Komponent | Note |
|-----------|------|
| `GET /api/auth/me` | ✅ — MeEndpoint, MeHandler |
| `POST /api/auth/change-password` | ✅ — ChangePasswordHandler, endpoint, tests |
| `DELETE /api/auth/logout` | ✅ — LogoutHandler, endpoint, tests |
| `GET /api/localization/{languageId}` | ✅ — GetLabelsEndpoint |
| `ISystemLogger` | ✅ — DefaultSystemLogger med UserId+CustomerId |
| `CurrentUserMiddleware` | ✅ — C_001+C_005 fikset |
| `IPermissionService` (DoesProfileHaveRole, DoesUserHaveRole) | ✅ |

---

## Slices — Phase 2

### P2-SLICE-001 — user_self_service (PRIORITY 1)

**Mål:** Brugeren kan se og opdatere sin profil, anmode om og bekræfte password reset.

| Opgave | Beskrivelse | Status |
|--------|-------------|--------|
| `PUT /api/user/update` | Opdater DisplayName + LanguageId | ❌ |
| `POST /api/user/password-reset-request` | Send reset-link via email (token i DB) | ❌ |
| `POST /api/user/password-reset-confirm` | Valider token + opdater password | ❌ |
| `UserProfilePage.razor` | Blazor-side: vis og rediger profil | ❌ |
| Tests | HTTP integration + unit | ❌ |

**Dependencies:** email_foundation (reset-link), ISystemLogger (ved token-oprettelse)

**Færdig når:** Bruger kan opdatere sprog, se profil, og gennemføre password reset end-to-end.

---

### P2-SLICE-002 — email_foundation (PRIORITY 2)

**Mål:** Email-infrastruktur der kan sende template-baserede mails (reset link, notifikationer).

| Opgave | Beskrivelse | Status |
|--------|-------------|--------|
| `EmailTemplates`-tabel + migration | Navn, Subject, BodyHtml, LanguageId | ❌ |
| `IEmailService` + `EmailService` | Send via SMTP/relay | ❌ |
| Template rendering | Erstat `{{token}}`, `{{name}}` etc. | ❌ |
| `AppSetting.SmtpHost` / `SmtpPort` | Konfiguration via AppSetting | ❌ |
| Tests | Unit test rendering, integration test send (mock SMTP) | ❌ |

**Bruges af:** password_reset_request (P2-SLICE-001)

**Færdig når:** Et email med token kan sendes fra password-reset-flow og verificeres i test.

---

### P2-SLICE-003 — admin_light (PRIORITY 3)

**Mål:** Administrator kan oprette brugere, tildele roller og profiler (minimal UI).

| Opgave | Beskrivelse | Status |
|--------|-------------|--------|
| `POST /api/admin/users` | Opret bruger (email + initial password) | ❌ |
| `POST /api/admin/users/{id}/roles` | Tildel UserRole | ❌ |
| `POST /api/admin/users/{id}/profiles` | Tildel profil-adgang | ❌ |
| `AdminUserListPage.razor` | Enkel liste over brugere under customer | ❌ |
| Tests | HTTP integration + unit | ❌ |

**Constraints:**
- Kræver `UserRole.ManageUsers` på kaldende bruger
- Ingen avancerede admin-features endnu (masseoprettelse, import, SAML)

**Færdig når:** Admin kan oprette en bruger og tildele adgang via API + minimal Blazor UI.

---

### P2-SLICE-004 — ui_foundation (PRIORITY 4)

**Mål:** Grundlæggende Blazor-skelet der kan navigere, vise loading states og fejlhåndtere.

| Opgave | Beskrivelse | Status |
|--------|-------------|--------|
| `NavigationMenu.razor` | Sidebar/top-nav med auth-guards | ❌ |
| `AppShell.razor` | Overordnet layout: nav + content + breadcrumb | ❌ |
| `LoadingOverlay.razor` | Delt loading-komponent (bool IsLoading) | ❌ |
| `ErrorAlert.razor` | Delt fejlvisning fra `Result<T>.Error` | ❌ |
| Auth-guards i nav | Skjul menupunkter uden rettigheder | ❌ |
| Blazor-side: Settings | Bruger kan sætte AppSettings (SuperAdmin only) | ❌ |
| Tests | E2E smoke tests for nav + loading | ❌ |

**Færdig når:** Alle sider bruger `AppShell`, alle loading states bruger `LoadingOverlay`.

---

## Golden Sample — Phase 2

Beviser alle Phase 2-domæner end-to-end:

```
1. Bruger logger ind (arvet fra SLICE-006)
2. Bruger opdaterer DisplayName → GET /api/auth/me returnerer nyt navn
3. Bruger anmoder om password reset → email token genereret i DB
4. Bruger bekræfter reset → logger ind med nyt password
5. Admin opretter ny bruger → ny bruger kan logge ind
6. Alle sider vises i korrekt sprog (LanguageId fra JWT)
```

**Gate:** Alle ovenstående flows dækket af HTTP integrationstests.

---

## Test-navngivningskonvention (gældende fra Phase 2)

```
tests/GreenAi.Tests/Http/{Domain}/           ← HTTP integration tests
  UserSelfService/UserSelfServiceTests.cs
  Email/EmailFoundationTests.cs
  AdminLight/AdminLightTests.cs

tests/GreenAi.Tests/Features/{Domain}/       ← Unit tests per handler
  UserSelfService/UpdateUserHandlerTests.cs

tests/GreenAi.E2E/{Domain}/                  ← E2E (Playwright) per feature
  UserSelfService/UserProfileE2ETests.cs
```

**SliceXXX-filer** (001–006): bevares som foundation-reference. Ingen nye `SliceXXX`-filer.

---

## Succeskriterium

```
dotnet build → 0 warnings
dotnet test  → N/N passed
Golden Sample Phase 2 → alle 6 flows grønne
R-PHASE2-001 → ingen forretningsdomæner introduceret
```

---

## Bygges IKKE i Phase 2

| Domæne | Årsag |
|--------|-------|
| SMS / messaging / delivery | Blokeret af R-PHASE2-001 |
| 2FA / SAML2 / Azure AD | Fase 3 |
| Impersonation | Fase 3 |
| activity_log | Dropper ind med messaging |
| Avanceret admin (masseimport, LDAP) | Fase 4 |
| CI/CD pipeline | Separat sprint |
