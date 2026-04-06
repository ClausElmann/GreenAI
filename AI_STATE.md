# AI_STATE — green-ai

> **AI: Læs denne VED RESET / SESSION START** — giver dig current codebase state på 30 sekunder.  
> Vedligeholdes af AI: opdatér efter enhver EXEC_ entry (samme operation som EXECUTION_MEMORY.md).

---

## Status

| | |
|---|---|
| Build | ✅ 0 warnings |
| Tests | 337 (GreenAi.Tests) + 9 governance (GreenAi.E2E, Category=Governance, ~160ms) |
| DB | `GreenAI_DEV` — `(localdb)\MSSQLLocalDB` |
| App | `http://localhost:5057` — start: `dotnet run --project src/GreenAi.Api` |

---

## Feature Inventory — src/GreenAi.Api/Features/

| Domain | Features |
|---|---|
| AdminLight | AssignProfile, AssignRole, CreateUser, ListSettings, SaveSetting |
| Auth | ChangePassword, GetProfileContext, Login, Logout, Me, RefreshToken, SelectCustomer, SelectProfile |
| CustomerAdmin | GetCustomerSettings, GetProfiles, GetUsers |
| Identity | ChangeUserEmail |
| Localization | BatchUpsertLabels, GetLabels |
| System | Health, Ping |
| UserSelfService | PasswordReset, UpdateUser |

---

## UI System — wwwroot/css/ (CSS cascade order)

```
1. MudBlazor.min.css       ← base reset
2. app.css                 ← --ga-* aliases → var(--color-*), layout tokens
3. design-tokens.css       ← SSOT: --color-*, --font-*, --space-*, --font-icon-*
4. greenai-skin.css        ← MudBlazor palette overrides
5. greenai-enterprise.css  ← tables, forms, enterprise density
6. portal-skin.css         ← 43 .ga-* utility classes
```

Primary colour: `--color-primary: #2563EB`  
MudTheme palette override: inline `<style id="greenai-palette-override">` i `MainLayout.razor`

---

## Active Work

*(ingen — DRY audit afsluttet 2026-04-06)*

---

## Last 5 Key Decisions

| Dato | Beslutning |
|---|---|
| 2026-04-06 | DRY audit: 3 redirect stubs slettet, README links rettet, greenai-ui-skin.md §2-4 → pointer til color-system.md |
| 2026-04-06 | Color SSOT migreret til `design-tokens.css` (--color-*). Bridge: `app.css` `--ga-primary: var(--color-primary)` |
| 2026-04-06 | 9 governance tests: `GreenAi.E2E/Governance/CssTokenComplianceTests.cs`, alle `Category=Governance` |
| 2026-04-06 | UI CSS cascade etableret (6-lag order ovenfor) |
| 2026-04-03 | Audit log pattern: V016_AuditLog.sql + ExecuteInTransactionAsync |

---

## SSOT Navigation (kompakt — fuld: `AI_WORK_CONTRACT.md` trigger-tabel)

| Emne | SSOT fil |
|---|---|
| Endpoint/API | `docs/SSOT/backend/patterns/endpoint-pattern.md` |
| Handler | `docs/SSOT/backend/patterns/handler-pattern.md` |
| SQL/Dapper | `docs/SSOT/database/patterns/dapper-patterns.md` |
| Auth/JWT | `docs/SSOT/identity/README.md` |
| Labels | `docs/SSOT/localization/guides/label-creation-guide.md` |
| UI tokens | `docs/SSOT/ui/color-system.md` |
| UI komponenter | `docs/SSOT/ui/component-system.md` |
| Test strategi | `docs/SSOT/testing/testing-strategy.md` |
| Governance rules | `docs/SSOT/governance/README.md` |

---

*Fuld task-log: `docs/SSOT/governance/EXECUTION_MEMORY.md`*
