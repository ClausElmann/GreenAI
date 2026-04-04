# Visual Analysis — Screenshot Export Pipeline

**Status:** Active  
**Last updated:** 2026-04-04  
**SSOT trigger:** `visual analysis` / `screenshot analysis` / `ui qa` / `playwright screenshots`

---

## Purpose

The visual analysis pipeline collects the latest Playwright screenshots from
multi-device visual tests and packages them into a structured ZIP for external
AI-based review (ChatGPT, Claude, or similar).

**No pixel-diff or image comparison runs inside the build** — the system is
export-only. Screenshots are produced, organised, and handed to an external
reviewer.

---

## Flow

```
Playwright visual tests
        │
        ▼
TestResults/Visual/current/{device}/*.png
        │
        ▼
ExportVisualAnalysisPackage (test trigger)
        │   calls VisualAnalysisExporter.ExportAsync()
        ▼
analysis-pack/
  desktop/  laptop/  tablet/  mobile/
  instructions.json
        │
        ▼
TestResults/Visual/analysis-pack.zip
        │
        ▼
Upload to ChatGPT / Claude
  → Paste analysis JSON response back
```

---

## Folder Structure

```
tests/GreenAi.E2E/
  VisualAnalysis/
    VisualAnalysisExporter.cs     ← Packager + ZIP creator
    ExportVisualAnalysisTests.cs  ← Test trigger

  TestResults/Visual/
    current/                      ← Written by Playwright (source)
      desktop/  laptop/  tablet/  mobile/
    baseline/                     ← Reference images (first run)
    analysis-pack/                ← Temp folder (deleted after zip)
    analysis-pack.zip             ← Final export artefact
```

---

## How to Trigger Export

### Step 1 — Run visual tests (produces screenshots)

```powershell
dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" --nologo
```

### Step 2 — Export analysis pack

```powershell
dotnet test tests/GreenAi.E2E --filter ExportVisualAnalysisPackage --nologo -v n
```

Output (visible in test log):

```
═══════════════════════════════════════════════════════════
  VISUAL ANALYSIS PACKAGE CREATED
═══════════════════════════════════════════════════════════
  ZIP : ...TestResults/Visual/analysis-pack.zip
  Files:
    desktop    5 screenshot(s)
    laptop     5 screenshot(s)
    tablet     5 screenshot(s)
    mobile     5 screenshot(s)
  Total: 20 screenshot(s)

  Next step:
    Upload the ZIP and its instructions.json to ChatGPT/Claude.
    Prompt: Analyse these screenshots per instructions.json.
═══════════════════════════════════════════════════════════
```

---

## instructions.json (generated inside ZIP)

```json
{
  "generated_at": "2026-04-04T...",
  "rules": [
    "no horizontal overflow",
    "no overlapping elements",
    "navigation must be accessible",
    "text must not be cut off",
    "interactive elements must be visible"
  ],
  "focus": ["layout stability", "responsive design", "usability"],
  "devices": [
    { "folder": "desktop", "viewport": "1920×1080" },
    { "folder": "laptop",  "viewport": "1366×768"  },
    { "folder": "tablet",  "viewport": "1024×768"  },
    { "folder": "mobile",  "viewport": "390×844"   }
  ],
  "response_format": {
    "description": "Return findings as JSON array",
    "example": [
      {
        "device":   "mobile",
        "file":     "dashboard.png",
        "issue":    "Description of the problem",
        "severity": "high|medium|low",
        "rule":     "which rule was violated"
      }
    ]
  }
}
```

---

## Example ChatGPT Usage

1. Unzip `analysis-pack.zip`
2. Upload all images and `instructions.json` to ChatGPT (or drag-drop to Claude)
3. Prompt:

> "Analyse these UI screenshots following the rules in instructions.json.
>  Return your findings as a JSON array matching the response_format."

---

## Example AI Response Format

```json
[
  {
    "device":   "mobile",
    "file":     "dashboard.png",
    "issue":    "Navigation toggle button is barely tappable — height ~24px",
    "severity": "high",
    "rule":     "interactive elements must be visible"
  },
  {
    "device":   "tablet",
    "file":     "overlay-nav-open.png",
    "issue":    "Nav panel overlaps main content without backdrop visible",
    "severity": "medium",
    "rule":     "no overlapping elements"
  }
]
```

---

## Error Cases

| Situation | Behaviour |
|---|---|
| No screenshots in `current/` | Test fails with: "No screenshots found. Run visual tests first." |
| Device folder missing | Skipped silently — other devices still exported |
| Fewer than expected files | Export succeeds with however many files exist |
| ZIP already exists | Overwritten |

---

## Files

| File | Purpose |
|---|---|
| `VisualAnalysis/VisualAnalysisExporter.cs` | Core logic: collect → pack → zip |
| `VisualAnalysis/ExportVisualAnalysisTests.cs` | Test trigger for `ExportVisualAnalysisPackage` |
| `Visual/VisualTestBase.cs` | Produces the screenshots (source) |
| `Visual/NavigationVisualTests.cs` | Defines which pages are captured |
