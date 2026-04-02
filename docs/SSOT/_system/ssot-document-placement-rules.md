# SSOT Document Placement Rules — green-ai

> **AI: Read this BEFORE creating any `.md` file.**

---

## Decision Tree

```
STOP — What area is this?
  ↓
Backend (endpoints, handlers, services, pipeline)?
  → docs/SSOT/backend/
  → patterns/ for reusable code patterns
  → guides/ for how-to workflows
  → architecture/ for design decisions

Database (schema, migrations, SQL, DbUp)?
  → docs/SSOT/database/
  → patterns/ for SQL conventions
  → migrations/ for migration workflow docs

Localization (labels, languages, ILocalizationService)?
  → docs/SSOT/localization/
  → guides/ for label creation rules
  → reference/ for shared label catalogs

Identity / Auth (JWT, ICurrentUser, permissions, tenant)?
  → docs/SSOT/identity/
  → architecture/ for auth flow diagrams
  → patterns/ for code patterns

Testing (unit tests, integration, DB fixture, coverage)?
  → docs/SSOT/testing/
  → patterns/ for test code patterns
  → guides/ for test running workflows

SSOT system itself (standards, placement rules, governance)?
  → docs/SSOT/_system/
```

---

## 5-Step Process

1. **Identify area** using tree above
2. **Check** `docs/SSOT/{area}/README.md` for existing subfolder
3. **Verify** file will be <450 lines when complete
4. **Create** file in `docs/SSOT/{area}/{subfolder}/topic-name.md`
5. **Update** `docs/SSOT/{area}/README.md` with link to new file

---

## What Does NOT Belong in SSOT

| Content                             | Correct location               |
| ----------------------------------- | ------------------------------ |
| Architecture overview               | `docs/ARCHITECTURE.md`         |
| Build status / progress             | `docs/STATUS.md`               |
| Technical decisions (ADRs)          | `docs/DECISIONS.md`            |
| Code quality / review notes         | `docs/CODE-REVIEW.md`          |
| Work-in-progress / exploratory      | `docs/knowledge/` (if created) |

---

**Last Updated:** 2026-04-02
