<#
.SYNOPSIS
    Parse TRX files from dotnet test and extract failed tests.
.DESCRIPTION
    Used after: dotnet test tests/GreenAi.Tests --logger trx
    Parses latest TRX in tests/GreenAi.Tests/TestResults/ and shows failures.
.PARAMETER TrxPath
    Path or glob to TRX file. Default: tests/GreenAi.Tests/TestResults/*.trx (newest)
.PARAMETER OutputFormat
    Table (default) or Json
.EXAMPLE
    pwsh -File scripts/testing/Get-FailedTestsFromTrx.ps1
    pwsh -File scripts/testing/Get-FailedTestsFromTrx.ps1 -TrxPath "tests/GreenAi.Tests/TestResults/myrun.trx"
#>

param(
    [string]$TrxPath = "",
    [ValidateSet("Table", "Json")]
    [string]$OutputFormat = "Table"
)

# Resolve TRX path — default to newest file in standard location
if (-not $TrxPath) {
    $repoRoot  = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $resultsDir = Join-Path $repoRoot "tests\GreenAi.Tests\TestResults"
    $newest = Get-ChildItem -Path $resultsDir -Filter "*.trx" -ErrorAction SilentlyContinue |
              Sort-Object LastWriteTime -Descending |
              Select-Object -First 1
    if (-not $newest) {
        Write-Host "❌ No TRX files found in $resultsDir" -ForegroundColor Red
        Write-Host "   Run: dotnet test tests/GreenAi.Tests --logger trx" -ForegroundColor Gray
        exit 1
    }
    $TrxPath = $newest.FullName
}
elseif ($TrxPath -match '\*') {
    $expanded = Get-ChildItem -Path $TrxPath -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1
    if (-not $expanded) {
        Write-Host "❌ No TRX files matched: $TrxPath" -ForegroundColor Red
        exit 1
    }
    $TrxPath = $expanded.FullName
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  TRX: $(Split-Path $TrxPath -Leaf)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

[xml]$trx = Get-Content $TrxPath -Raw

# Summary counters
$counters = $trx.TestRun.ResultSummary.Counters
if ($counters) {
    $total   = $counters.total
    $passed  = $counters.passed
    $failed  = $counters.failed
    $color   = if ($failed -eq '0') { "Green" } else { "Red" }
    Write-Host "  Total: $total  |  Passed: $passed  |  Failed: $failed" -ForegroundColor $color
    Write-Host ""
}

# Collect failures
$failures = @()
$results = $trx.TestRun.Results.UnitTestResult
if (-not $results) { $results = $trx.SelectNodes("//UnitTestResult") }

foreach ($r in @($results)) {
    if ($r.outcome -ne "Failed") { continue }

    $msg   = $r.Output?.ErrorInfo?.Message ?? ""
    $stack = $r.Output?.ErrorInfo?.StackTrace ?? ""

    $httpStatus = $null
    if ($msg -match 'HTTP (\d{3})|StatusCode: (\d{3})') {
        $httpStatus = if ($matches[1]) { $matches[1] } else { $matches[2] }
    }

    $failures += [PSCustomObject]@{
        TestName   = $r.testName
        Message    = $msg.Trim()
        HttpStatus = $httpStatus
        TopStack   = ($stack -split "`n" | Select-Object -First 1).Trim()
    }
}

if ($failures.Count -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
    exit 0
}

if ($OutputFormat -eq "Json") {
    $failures | ConvertTo-Json -Depth 3
    exit 1
}

# Table output
foreach ($f in $failures) {
    Write-Host "❌ $($f.TestName)" -ForegroundColor Red
    if ($f.HttpStatus) {
        Write-Host "   HTTP: $($f.HttpStatus)" -ForegroundColor Yellow
    }
    if ($f.Message) {
        $preview = $f.Message.Substring(0, [Math]::Min(200, $f.Message.Length))
        Write-Host "   Msg: $preview" -ForegroundColor Gray
    }
    if ($f.TopStack) {
        Write-Host "   At:  $($f.TopStack.Substring(0, [Math]::Min(120, $f.TopStack.Length)))" -ForegroundColor DarkGray
    }
    Write-Host ""
}

Write-Host "  $($failures.Count) test(s) failed." -ForegroundColor Red
exit 1
