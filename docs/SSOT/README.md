# SSOT — green-ai

> Single Source of Truth documentation. Each topic exists in exactly ONE authoritative location.

---

## Areas

| Area                                   | Topics covered                                                |
| -------------------------------------- | ------------------------------------------------------------- |
| [backend/](backend/README.md)          | Minimal API endpoints, handlers, validators, pipeline         |
| [database/](database/README.md)        | DbUp migrations, SQL patterns, schema conventions             |
| [localization/](localization/README.md)| Labels table, ILocalizationService, shared.* keys             |
| [identity/](identity/README.md)        | Custom JWT auth, ICurrentUser, tenant isolation, permissions  |
| [testing/](testing/README.md)          | xUnit v3, DatabaseFixture, Respawn, NSubstitute patterns      |
| [governance/](governance/README.md)    | Build plan, red threads, execution protocol, self-optimization|
| [ui/](ui/README.md)                    | Blazor components, MudBlazor conventions, UI navigation models|
| [_system/](_system/ssot-standards.md) | SSOT standards, file size rules, placement decision tree      |

---

## Rules

1. **One topic, one file.** No duplication across areas.
2. **File size:** <450 lines ideal, <600 HARD LIMIT.
3. **All files have:** `**Last Updated:** YYYY-MM-DD` in header or footer.
4. **Link, don't copy.** If info exists elsewhere, link to it.

---

## Adding New Documentation

Before creating any `.md` file:

1. Read `_system/ssot-document-placement-rules.md` (30 seconds)
2. Identify area → check area `README.md` for subfolder
3. Create file in correct location
4. Update area `README.md` with link

---

**Last Updated:** 2026-04-03
