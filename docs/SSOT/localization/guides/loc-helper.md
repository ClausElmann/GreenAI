# Loc Helper — LocalizationContext

```yaml
id: loc_helper
type: guide
ssot_source: docs/SSOT/localization/guides/loc-helper.md
red_threads: []
applies_to: ["Components/**/*.razor", "Features/**/*.cs"]
enforcement: never hardcode strings — use @Loc.Get(...)
```

> How to use `ILocalizationContext` for sync label access in Blazor components.

**Last Updated:** 2026-04-02

---

## Pattern

```razor
@inject ILocalizationContext Loc
@inject ICurrentUser CurrentUser

@code {
    protected override async Task OnInitializedAsync()
    {
        await Loc.EnsureLoadedAsync(CurrentUser.LanguageId);
    }
}
```

Then in markup:

```razor
<MudButton>@Loc.Get("shared.SaveButton")</MudButton>
<MudButton>@Loc.Get("shared.CreateEntityButton", "kunde")</MudButton>
```

---

## Rules

```
✅ @inject ILocalizationContext Loc  — always this interface
✅ EnsureLoadedAsync() in OnInitializedAsync — always call once
✅ Loc.Get("key") — sync, safe to call from markup
✅ Loc.Get("key", arg0) — replaces {0} with arg0
❌ NEVER hardcode strings in Blazor — always use Loc.Get
❌ NEVER inject ILocalizationService directly in Razor — use ILocalizationContext
```

---

## How It Works

`LocalizationContext` is Scoped (one instance per Blazor circuit).  
`EnsureLoadedAsync()` calls `ILocalizationService.GetAllAsync(languageId)` once and caches into a dictionary.  
Subsequent `Get()` calls are dictionary lookups — no DB queries.  
Fail-open: if a key is missing, the key string itself is returned.

---

## Where to Add Labels

See [label-creation-guide.md](label-creation-guide.md) for the decision tree: `shared.*` vs feature-specific keys.

---

**Last Updated:** 2026-04-02
