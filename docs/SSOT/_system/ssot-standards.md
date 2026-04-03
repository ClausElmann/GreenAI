# SSOT Standards — green-ai

```yaml
id: ssot_standards
type: convention
ssot_source: docs/SSOT/_system/ssot-standards.md
red_threads: []
applies_to: ["docs/SSOT/**"]
enforcement: check-file-sizes.ps1 — hard limit 600 lines per file
```

> Rules for all documentation under `docs/SSOT/`.

---

## File Size Limits

| Status          | Lines   | Action                              |
| --------------- | ------- | ----------------------------------- |
| IDEAL           | <450    | None required                       |
| WARNING         | 450–600 | Consider splitting into sub-topics  |
| CRITICAL        | >600    | **MUST split before adding content** |

**Enforcement:** `scripts/governance/check-file-sizes.ps1 -Area all`

---

## DRY / SSOT Principle

- ✅ ONE authoritative file per topic
- ✅ Link to existing doc instead of copying content
- ❌ NEVER copy-paste content between files
- ❌ NEVER create "summary" versions of existing docs

---

## Required Header/Footer

Every SSOT file must end with:

```markdown
**Last Updated:** YYYY-MM-DD
```

---

## Naming Conventions

- ✅ `lowercase-with-hyphens.md` — e.g. `endpoint-pattern.md`
- ✅ `README.md` — index file per area (standard casing)
- ❌ `PascalCase.md`
- ❌ `UPPER_SNAKE.md`

---

## Subfolder Conventions

| Subfolder       | Purpose                                   |
| --------------- | ----------------------------------------- |
| `patterns/`     | Reusable code patterns with examples      |
| `guides/`       | How-to guides, step-by-step workflows     |
| `architecture/` | Design decisions, flow diagrams           |
| `conventions/`  | Naming rules, coding standards            |
| `reference/`    | Checklists, catalogs, quick-reference     |

---

**Last Updated:** 2026-04-02
