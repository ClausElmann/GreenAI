# scripts/run-ui-autofix.ps1
# UI Auto-Fix entrypoint — runs full quality gate loop.
# See: docs/SSOT/testing/ui-auto-fix-protocol.md
#
# Usage (inline terminal, no .ps1 file needed for one-off runs):
#   $env:GREENAI_ACCESSIBILITY_GATES="true"
#   dotnet test tests/GreenAi.E2E --filter "FullyQualifiedName~Visual" -v n
#
# This script is the registered entrypoint for the ui_auto_fix tool.

param(
    [int]    $MaxIterations = 3,
    [string] $Filter        = "FullyQualifiedName~Visual",
    [switch] $Headless
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$env:GREENAI_ACCESSIBILITY_GATES = "true"
if ($Headless) { $env:GREENAI_VISUAL_HEADLESS = "true" }

$projectPath  = "c:\Udvikling\green-ai\tests\GreenAi.E2E\GreenAi.E2E.csproj"
$outputDir    = "c:\Udvikling\green-ai\TestResults\Visual"
$failuresFile = Join-Path $outputDir "ui-failures.json"

New-Item -ItemType Directory -Force $outputDir | Out-Null

Write-Host "=== UI Auto-Fix Loop (max $MaxIterations iterations) ===" -ForegroundColor Cyan

for ($i = 1; $i -le $MaxIterations; $i++) {
    Write-Host "`n--- Iteration $i/$MaxIterations ---" -ForegroundColor Yellow

    $logFile = Join-Path $env:TEMP "greenai-ui-autofix-run$i.txt"
    dotnet test $projectPath --filter $Filter -v n 2>&1 | Tee-Object -FilePath $logFile
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "`n✅ All visual tests PASSED on iteration $i" -ForegroundColor Green
        Remove-Item $failuresFile -Force -ErrorAction SilentlyContinue
        exit 0
    }

    Write-Host "`n⚠️  Failures detected on iteration $i — parsing output..." -ForegroundColor Yellow

    # Parse failures from test log into structured JSON
    $content  = Get-Content $logFile -Raw
    $failures = @()

    if ($content -match "color-contrast") {
        $failures += @{ type = "contrast"; selector = ".mud-secondary-text, .mud-text-secondary"; issue = "WCAG AA contrast violation"; suggestedFix = "Ensure palette override in layout after MudThemeProvider" }
    }
    if ($content -match "touch target") {
        $failures += @{ type = "touch-target"; selector = ".mud-button-root, .mud-icon-button"; issue = "Mobile tap target below 44x44px"; suggestedFix = "Add min-height:44px in @media (max-width:768px) in greenai-skin.css" }
    }
    if ($content -match "document-title") {
        $failures += @{ type = "document-title"; selector = "html > head > title"; issue = "Missing <title> element"; suggestedFix = "Add <title>GreenAI</title> in App.razor" }
    }
    if ($content -match "Design token violations") {
        $failures += @{ type = "contrast"; selector = ":root --ga-*"; issue = "CSS design tokens missing or incorrect"; suggestedFix = "Verify app.css is loaded before greenai-skin.css" }
    }
    if ($content -match "outline-width") {
        $failures += @{ type = "focus"; selector = ":focus-visible"; issue = "Focus ring missing or too thin"; suggestedFix = "Add :focus-visible outline rule in greenai-skin.css" }
    }

    $failureDoc = @{ generated_at = (Get-Date -Format "o"); iteration = $i; failures = $failures }
    $failureDoc | ConvertTo-Json -Depth 5 | Set-Content $failuresFile -Encoding UTF8
    Write-Host "  → Wrote $($failures.Count) failure(s) to $failuresFile"
}

Write-Host "`n❌ Still failing after $MaxIterations iterations. See: $failuresFile" -ForegroundColor Red
exit 1
