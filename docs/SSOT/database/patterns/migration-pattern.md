# Migration Pattern — green-ai

> DbUp migration file conventions.

**Last Updated:** 2026-04-02

---

## Naming Convention

```
V001_InitialSchema.sql
V002_Users.sql
V003_Customers.sql
...
V013_SeedSharedLabels.sql
```

**Rules:**
- `V` prefix + 3-digit zero-padded number + underscore + PascalCase description
- Numbers are sequential, never reused
- Files are in: `src/GreenAi.Api/Database/Migrations/`

---

## File Structure

```sql
-- V013_SeedSharedLabels.sql
-- Purpose: Seed shared.* localization labels (DA + EN)
-- Depends: V011 (Labels table)

SET NOCOUNT ON;

INSERT INTO dbo.Labels (ResourceName, ResourceValue, LanguageId)
VALUES
    ('shared.SaveButton',   'Gem',       1),  -- DA
    ('shared.SaveButton',   'Save',      3),  -- EN
    ('shared.CancelButton', 'Annuller',  1),
    ('shared.CancelButton', 'Cancel',    3);
```

---

## "Depend On" Rule

Every migration that adds a column, FK, or inserts data into a table from an earlier migration must include a comment:

```sql
-- Depends: V011 (Labels table)
```

---

## Rollback Strategy

DbUp does NOT support rollback. Instead:

- Development: drop and recreate DB, re-run all migrations
- Production: write a new `VXxx_Rollback_Description.sql` that reverses the change

---

## DatabaseMigrator Registration

Migrations run automatically on startup via `DatabaseMigrator.Run(connectionString)` in `Program.cs`. New `.sql` files are picked up automatically as embedded resources.

---

**Last Updated:** 2026-04-02
