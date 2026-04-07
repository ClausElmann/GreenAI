# scripts/run-ui-autofix.ps1
# UI Governance Loop — Slice 5
# Reads governance-delta.json after each test run and applies stop conditions.
#
# Stop conditions (in priority order):
#   SUCCESS:    score >= 80 AND scoreDelta >= 0
#   REGRESSION: scoreDelta < 0
#   STUCK:      scoreDelta == 0 for 2 consecutive iterations
#   TIMEOUT:    iterations > maxIterations

$maxIterations = 3
$iteration     = 1
$stuckCount    = 0

$deltaPath  = Join-Path $PSScriptRoot "..\tests\GreenAi.E2E\TestResults\governance-delta.json"
$reportPath = Join-Path $PSScriptRoot "..\tests\GreenAi.E2E\TestResults\governance-report.json"
$fixPath    = Join-Path $PSScriptRoot "..\tests\GreenAi.E2E\TestResults\copilot-fix-input.json"

Write-Host "Starting UI Governance Loop..."

while ($iteration -le $maxIterations) {

    Write-Host ""
    Write-Host "Iteration $iteration"

    dotnet test tests/GreenAi.E2E --filter "Category=UIGovernance" --nologo

    if (!(Test-Path $deltaPath)) {
        Write-Host "Delta file missing"
        exit 1
    }

    $delta      = Get-Content $deltaPath | ConvertFrom-Json
    $score      = $delta.currentScore
    $deltaScore = $delta.scoreDelta

    Write-Host "Score: $score (delta: $deltaScore)"

    # ── Generate copilot-fix-input.json ───────────────────────────────────────
    if (Test-Path $reportPath) {
        $report   = Get-Content $reportPath | ConvertFrom-Json
        $failures = $report.rules | Where-Object { $_.passed -eq $false }

        $priorityOrder = @{ "critical" = 1; "major" = 2; "minor" = 3 }
        $sorted = $failures | Sort-Object { $priorityOrder[$_.severity] }

        $fixTarget = "NONE"
        if ($sorted.Count -gt 0) {
            $fixTarget = switch ($sorted[0].severity) {
                "critical" { "TOP_CRITICAL_FIRST" }
                "major"    { "TOP_MAJOR_FIRST" }
                default    { "TOP_MINOR_FIRST" }
            }
        }

        $index = 1
        $fixes = @($sorted | ForEach-Object {
            $entry = @{ priority = $index; ruleKey = $_.ruleKey; severity = $_.severity; message = $_.message }
            $index++
            $entry
        })

        @{ iteration = $iteration; score = $score; fixTarget = $fixTarget; fixes = $fixes } |
            ConvertTo-Json -Depth 5 | Set-Content -Path $fixPath
        Write-Host "Fix input: $fixTarget ($($fixes.Count) failure(s)) → $fixPath"
    }

    # SUCCESS
    if ($score -ge 80 -and $deltaScore -ge 0) {
        Write-Host "SUCCESS"
        exit 0
    }

    # REGRESSION
    if ($deltaScore -lt 0) {
        Write-Host "REGRESSION DETECTED"
        exit 1
    }

    # STUCK DETECTION
    if ($deltaScore -eq 0) {
        $stuckCount++
        if ($stuckCount -ge 2) {
            Write-Host "STUCK DETECTED"
            exit 1
        }
    } else {
        $stuckCount = 0
    }

    $iteration++
}

Write-Host "MAX ITERATIONS REACHED"
exit 1
