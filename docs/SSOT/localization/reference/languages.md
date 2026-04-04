# Languages — green-ai

> **Autoritet:** SSOT for sprog-konfiguration i green-ai.
> **Kilde:** V010_RolesAndLanguages.sql + analyse af sms-service-domæne.

---

## Seede sprog (Languages-tabel)

green-ai understøtter 6 sprog. Seedes i V010_RolesAndLanguages.sql med IDENTITY_INSERT:

| Id | Name | LanguageCulture | UniqueSeoCode | Iso639_1 | DisplayOrder |
|----|------|-----------------|---------------|----------|--------------|
| 1 | Danish | da-DK | da | da | 1 |
| 2 | Swedish | sv-SE | sv | sv | 2 |
| 3 | English | en-GB | en | en | 3 |
| 4 | Finnish | fi-FI | fi | fi | 4 |
| 5 | Norwegian | nb-NO | nb | nb | 5 |
| 6 | German | de-DE | de | de | 6 |

**Id=1 (Danish)** er bootstrap-default — bruges som `DEFAULT 1` i `UserCustomerMembership.LanguageId`.

---

## Brug i green-ai

`LanguageId` er bruger-per-kunde kontekst — placeret på `UserCustomerMembership`, ikke i JWT.

```csharp
// LanguageId hentes fra DB i CurrentUserMiddleware
// Allerede tilgængeligt via ICurrentUser.LanguageId (int)

// Brug i handler:
var labels = await _localization.GetAllForLanguageAsync(currentUser.LanguageId);

// Brug i Blazor:
@Loc.Get("shared.save")  // ILocalizationContext bruger ICurrentUser.LanguageId automatisk
```

---

## Regler

```
✅ LanguageId = int (1..6) — bruger ICurrentUser.LanguageId
✅ Id=1 (da) er default når ingen sprogpræference er sat
✅ Alle handlers der returnerer labels: brug currentUser.LanguageId
❌ LanguageId er IKKE i JWT — hent fra ICurrentUser (sat af middleware)
❌ Aldrig hardcode LanguageId som int — brug LanguageIds-konstanter
```

```csharp
// green-ai LanguageIds (strongly typed konstanter)
public static class LanguageIds
{
    public const int Danish   = 1;
    public const int Swedish  = 2;
    public const int English  = 3;
    public const int Finnish  = 4;
    public const int Norwegian = 5;
    public const int German   = 6;
}
```

---

## Fase 1 minimumskrav (SLICE-004)

Labels skal seedede i minimum DK (Id=1) og NO (Id=5) for at golden sample kan bevises.
