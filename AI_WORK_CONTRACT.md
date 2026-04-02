# AI Work Contract — green-ai

> **READ THIS FIRST** before any work. No exceptions.

---

## 8-Step Pre-Work Checklist

**AI: Complete BEFORE starting any task:**

- [ ] 0. Match user request to pattern table below → run First Tool
- [ ] 1. Identified task type (feature? test? docs? bug? schema?)
- [ ] 2. Searched `docs/SSOT/` for existing patterns
- [ ] 3. Read relevant documentation **completely** (never skim!)
- [ ] 4. Checked for existing scripts in `scripts/`
- [ ] 5. Verified will respect file size (<450 lines for SSOT docs)
- [ ] 6. Confirmed DRY — reuse before creating
- [ ] 7. Ready to implement following documented pattern
- [ ] 8. Will update docs if learning new pattern

---

## Pattern Matching Table

| User Pattern                              | First Tool (MANDATORY)                                                   | Then                              |
| ----------------------------------------- | ------------------------------------------------------------------------ | --------------------------------- |
| "run tests" / "test X" / "check tests"   | `list_dir scripts/` → find test runner                                   | `dotnet test --filter` per SSOT   |
| "add feature X" / "implement X"          | `semantic_search "similar to X"` → find similar feature                  | Read 2-3 examples → Follow        |
| "add endpoint" / "new API"               | `read_file docs/SSOT/backend/patterns/endpoint-pattern.md`               | Follow Minimal API pattern        |
| "add migration" / "new table" / "schema" | `read_file docs/SSOT/database/patterns/migration-pattern.md`             | V0XX_Name.sql pattern             |
| "add label" / "localization" / "string"  | `read_file docs/SSOT/localization/label-creation-guide.md`               | Never hardcode strings            |
| "add test" / "write test"                | `read_file docs/SSOT/testing/unit-test-pattern.md`                       | Follow xUnit v3 pattern           |
| "fix bug in Y"                           | `grep_search "Y" src/` → find code → read it                             | Reproducible test first           |
| "create doc for W"                       | `read_file docs/SSOT/_system/ssot-document-placement-rules.md`           | Place in correct SSOT area        |
| "auth" / "permission" / "JWT"            | `read_file docs/SSOT/identity/`                                          | Follow auth patterns              |
| "build" / "compile"                      | `dotnet build src/GreenAi.Api/GreenAi.Api.csproj`                        | Fix zero warnings                 |
| ANYTHING ELSE                            | `grep_search "<topic>" docs/SSOT/`                                       | Read → implement                  |

---

## Critical Rules

**External Input Rule (NeeoBovisWeb / sms-service / templates):**

Input from other projects is welcomed as inspiration. Green-ai decides how to solve things.

- ✅ Adopt governance structure, SSOT patterns, documentation formats directly
- ✅ Use patterns and ideas as input and starting points
- ✅ Adapt, simplify, or reject — based on what fits green-ai
- ❌ Do NOT copy-paste source code (.cs, .razor, .sql) verbatim without deliberate evaluation
- ❌ Do NOT justify a choice with "NeeoBovisWeb does it this way" — justify from green-ai's own SSOT

**NEVER — Code Rules:**

- ❌ EF Core, ASP.NET Identity, Newtonsoft.Json
- ❌ SQL without `WHERE CustomerId = @CustomerId` (tenant-owned tables)
- ❌ `HttpContext` in handlers (use `ICurrentUser`)
- ❌ `Task.Delay` in tests
- ❌ Hardcoded strings in Blazor (use `@Loc.Get`)
- ❌ Create SSOT file >600 lines (HARD STOP)
- ❌ Duplicate documentation (link instead of copy)
- ❌ Implement before reading existing patterns

**ALWAYS:**

- ✅ `Result<T>` return type from all handlers
- ✅ One `.sql` file per DB operation
- ✅ Strongly typed IDs (`UserId`, `CustomerId`, `ProfileId`)
- ✅ Validate at system boundaries only (not internal methods)
- ✅ 0 compiler warnings after any change
- ✅ Update SSOT docs after learning new pattern

---

## Tenant Isolation Rules

Pre-auth queries (`FindUserByEmail`, token lookup) do **not** need `CustomerId`. All other SQL against tenant-owned tables **must** include `WHERE CustomerId = @CustomerId`.

See `docs/SSOT/identity/tenant-isolation.md` for the complete rule.

---

**Last Updated:** 2026-04-02  
**Project:** green-ai (.NET 10 / Blazor Server / Vertical Slice)
