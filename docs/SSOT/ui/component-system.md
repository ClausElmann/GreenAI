# Component System — green-ai Portal

**SSOT for CSS utility classes** that replace inline `Style=` attributes in Razor components.

> Governed by: `tests/GreenAi.E2E/Governance/CssTokenComplianceTests.cs`  
> Defined in: `wwwroot/css/portal-skin.css` (Component System section)  
> All values use `--space-*` / `--font-*` tokens from `wwwroot/css/design-tokens.css`.

Run enforcement: `dotnet test --filter "Category=Governance"` → 9 tests, ~130ms, no browser.

---

## Governance Tests (9 total — all Category=Governance)

| Test | Type | What it checks |
|---|---|---|
| `NoCssSourceFiles_ContainHardcodedColorValues` | FAIL | No hex/rgb() in our CSS except inside token definitions |
| `CssSourceFiles_ExistAndAreReachable` | FAIL | Core CSS files are present |
| `PortalSkin_FontSizes_UseTokensNotHardcodedValues` | FAIL | `font-size` in portal-skin.css must use `var(--font-*)` |
| `DesignTokens_ContainsRequiredTypographyAndSpacingTokens` | FAIL | All 22 required tokens present in design-tokens.css |
| `RazorFiles_DoNotContainBannedInlineStyles` | FAIL | Banned `Style=` patterns: text-align:right, margin:0, font-size:, max-width:280px..., color:#, background:# |
| `MudTables_MustUseDenseMode` | FAIL | Every `<MudTable` must have `Dense="true"` |
| `AppButtons_PlainHtml_MustHaveGaClass` | FAIL | `<button>` outside Layout/ must carry `ga-btn-*` class |
| `CssOutlineNone_MustHaveFocusReplacement` | FAIL | `outline: none` requires `box-shadow` or `border-color` in same rule block. Exempt: h1-h6. |
| `MudButton_ColorError_Advisory` | ADVISORY (always passes) | Logs `[ADVISORY]` to console when `MudButton Color.Error` found — no CI break |

---

## Table Column Helpers

| Class | Replaces | Use on |
|---|---|---|
| `ga-col-numeric` | `Style="text-align:right"` | `MudTh`, `MudTd` |
| `ga-text-cell-truncate` | `Style="max-width:280px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap"` | `MudText` inside `MudTd` |

```razor
<MudTh Class="ga-col-numeric">Modtagere</MudTh>
<MudTd DataLabel="Modtagere" Class="ga-col-numeric">@context.Count.ToString("N0")</MudTd>
```

---

## Chip Helpers

| Class | Replaces | Use on |
|---|---|---|
| `ga-chip-reset` | `Style="margin:0"` | `MudChip` |

```razor
<MudChip T="string" Size="Size.Small" Variant="Variant.Outlined" Class="ga-chip-reset">
    @context.Role
</MudChip>
```

---

## Icon Size Helpers

All applied to `MudIcon`. Font-size drives scale because the SVG inside uses `1em`.

| Class | Font-size | Opacity | Replaces |
|---|---|---|---|
| `ga-icon-xl` | 3rem | — | `Style="font-size:3rem"` |
| `ga-icon-xl-dim` | 3rem | .8 | `Style="font-size:3rem;opacity:.8"` |
| `ga-icon-xl-faded` | 4rem | .4 | `Style="font-size:4rem;opacity:.4"` |
| `ga-icon-2xl` | 5rem | — | `Style="font-size:5rem"` |
| `ga-icon-2xl-faded` | 5rem | .2 | `Style="font-size:5rem;opacity:.2"` |

```razor
<MudIcon Icon="@Icons.Material.Filled.ManageSearch" Class="ga-icon-2xl-faded" />
<MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" Class="ga-icon-2xl" />
```

---

## Button Helpers

| Class | Description |
|---|---|
| `ga-btn-primary` | Styled primary button for HTML `<button>` elements (not MudButton) |

---

## Surface / Container Helpers

| Class | Description |
|---|---|
| `ga-surface` | White card surface with border and border-radius |
| `ga-border` | Light border only (no background) |
| `ga-section` | Adds `margin-bottom: var(--space-5)` for vertical rhythm |
| `ga-page-content` | Page padding (`var(--space-6)`) |
| `ga-form-group` | Form field spacing (`margin-bottom: var(--space-4)`) |

---

## Spacing / Text Utilities

| Class | Value |
|---|---|
| `ga-space-sm` | `margin-bottom: var(--space-2)` (8px) |
| `ga-space-md` | `margin-bottom: var(--space-4)` (16px) |
| `ga-space-lg` | `margin-bottom: var(--space-5)` (24px) |
| `ga-text-sm` | `font-size: var(--font-sm)` (14px) |
| `ga-text-md` | `font-size: var(--font-md)` (16px) |
| `ga-text-lg` | `font-size: var(--font-lg)` (18px) |
| `ga-text-secondary` | `color: var(--color-text-secondary)` |

---

## Rules

- ❌ **Never** use `Style="text-align:right"` → `ga-col-numeric`
- ❌ **Never** use `Style="margin:0"` on chips → `ga-chip-reset`
- ❌ **Never** use `Style="font-size:Xrem"` on icons → `ga-icon-*` helpers
- ❌ **Never** use `Style="max-width:280px;overflow:hidden..."` → `ga-text-cell-truncate`
- ✅ `style="display:none"` (lowercase, functional) is **allowed** on `InputFile`
- ✅ `Style="@($"color:{dynamicVar}")"` (dynamic computed) is **allowed**

---

## Governance

`CssTokenComplianceTests.RazorFiles_DoNotContainBannedInlineStyles` (Category=Governance) enforces these rules.  
Run with filter: `dotnet test --filter "Category=Governance"`

---

*Last updated: 2026-04-04*
