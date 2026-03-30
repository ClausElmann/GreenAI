# Beslutningslog

## [2026-03-30] DbUp over EF migrationer

**Beslutning:** DbUp med embedded `.sql`-filer, kører ved app-start  
**Begrundelse:** Ingen ORM. SQL-scripts er versionerede, reviewable og deterministiske.  
**Konvention:** `Database/Migrations/V001_*.sql`, `V002_*.sql` osv. Numbered prefix styrer rækkefølgen.  
**DB:** `(localdb)\MSSQLLocalDB` / `greenai_dev` i development.

---

## [2026-03-30] Initial scaffolding

**Beslutning:** Vertical slice architecture med co-located Blazor pages  
**Begrundelse:** AI context-vindue passer til én slice ad gangen. Alt hvad der hører til en feature er i én mappe.  
**Alternativer overvejet:** Layered (Controller/Service/Repository) — fravalgt fordi AI kræver 4+ filer i kontekst per ændring.

---

## [2026-03-30] Dapper over EF Core

**Beslutning:** Kun Dapper, ingen EF Core  
**Begrundelse:** SQL er eksplicit, versionerbar og AI-læsbar. EF's LINQ-til-SQL er uforudsigelig for AI-genereret kode.  
**Risiko:** Ingen change tracking, ingen global query filters → tenant WHERE-klausul skal altid skrives eksplicit.  
**Mitigering:** `IDbSession` wrapper + én `.sql`-fil per operation.

---

## [2026-03-30] Custom JWT over ASP.NET Identity

**Beslutning:** Custom JWT via `JwtTokenService` (ikke skabt endnu)  
**Begrundelse:** ASP.NET Identity forvirrer AI-prompts. Alt auth skal være eksplicit og traceable.  
**Konvention:** `ICurrentUser` er den eneste måde at tilgå bruger-identity i handlers og components.

---

## [2026-03-30] Strongly-typed IDs

**Beslutning:** `UserId`, `CustomerId`, `ProfileId` som separate record structs  
**Begrundelse:** AI-genereret kode blander int-IDs. Compiler-fejl er bedre end runtime-fejl.

---

## [2026-03-30] Result<T> over exceptions

**Beslutning:** Alle handlers returnerer `Result<T>` — kaster aldrig for business-fejl  
**Begrundelse:** Eksplicit kontrakt. AI kan se alle mulige udfald uden at læse exception-dokumentation.

---

<!-- Tilføj nye beslutninger øverst under en ny dato-header -->
