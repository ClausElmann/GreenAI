# Code Review Checklist

> **Canonical:** SSOT for pre-commit and PR review requirements in green-ai.

```yaml
id: code_review_checklist
type: rule
version: 1.0.0
last_updated: 2026-04-06
ssot_source: docs/SSOT/governance/code-review-checklist.md
red_threads: [result_pattern, auth_flow, tenant_isolation, sql_embedded]
```

---

## Automated (run before submitting)

```powershell
# Build — must be zero errors, zero warnings
dotnet build src/GreenAi.Api/GreenAi.Api.csproj -v q

# Tests — must all pass
dotnet test tests/GreenAi.Tests -v q

# Governance — must print VALIDATION PASSED
& ".\scripts\governance\Validate-GreenAiCompliance.ps1"
```

---

## Manual Checklist

### Every Feature

- [ ] Has an entry in `feature-contract-map.json`
- [ ] `sql_files` paths in contract map match actual files on disk
- [ ] `pipeline_markers` reflect actual marker interfaces on Command/Query
- [ ] At least one test (unit or integration) registered and passing

### Commands / Queries

- [ ] `strongly_typed_ids`: no raw `int UserId`, `int CustomerId`, `int ProfileId` parameters
- [ ] `auth=authenticated`: has `IRequireAuthentication` marker
- [ ] `auth=profile`: has BOTH `IRequireAuthentication` AND `IRequireProfile` markers
- [ ] No manual `IsAuthenticated` check in handler body (use pipeline markers)

### SQL

- [ ] One `.sql` file per DB operation
- [ ] All tenant-scoped queries use `WHERE CustomerId = @CustomerId`
- [ ] No inline SQL strings in `.cs` (must use `SqlLoader.Load<T>`)
- [ ] No `RowVersion` in `SET` clause

### Blazor / UI

- [ ] No hardcoded strings — all via `@Loc.Get("key")`
- [ ] `EnsureLoadedAsync` called in layout or page `OnInitializedAsync`
- [ ] No `IMediator` injection in `Components/` (only in `Pages/`)
- [ ] All interactive elements have `data-testid` attribute
- [ ] No Bootstrap CSS classes (use MudBlazor)
- [ ] `MudChip` has `T="string"` attribute

### Handlers

- [ ] Returns `Result<T>` — never `void`, never throws for business errors
- [ ] All error codes registered in `ResultExtensions.cs`
- [ ] No `ICurrentUser` access without `IRequireAuthentication` pipeline marker

### Localization

- [ ] New label keys added to `V017_SeedLabels*.sql` migration
- [ ] Both DA (LanguageId=1) and EN (LanguageId=3) provided
- [ ] `shared-labels-reference.md` updated if new `shared.*` keys added

---

## Forbidden Patterns (immediate block)

```
❌ EF Core / LINQ-to-SQL
❌ ASP.NET Identity
❌ Newtonsoft.Json
❌ HttpContext in handlers
❌ Task.Delay in tests
❌ ProfileId(0) — never issue token with zero ProfileId
❌ SSOT file > 600 lines
```

---

**Last Updated:** 2026-04-06
