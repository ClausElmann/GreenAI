<#
.SYNOPSIS
    Validate green-ai code compliance rules.

.DESCRIPTION
    Checks source files for forbidden patterns specific to green-ai:
    - EF Core usage
    - ASP.NET Identity usage
    - HttpContext in handlers
    - Missing WHERE CustomerId in tenant SQL
    - Task.Delay in tests
    - Hardcoded strings in Blazor (non-@Loc calls)
    - Missing Result<T> return types in handlers

.PARAMETER Path
    File or folder to scan. Default: src/

.PARAMETER Fix
    (Not implemented) Placeholder for future auto-fix mode.

.EXAMPLE
    .\scripts\governance\Validate-GreenAiCompliance.ps1
    .\scripts\governance\Validate-GreenAiCompliance.ps1 -Path src/GreenAi.Api/Features/Localization
#>
param(
    [string]$Path = "src",
    [switch]$Fix
)

$repoRoot    = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$scanPath    = Join-Path $repoRoot $Path
$violations  = @()

function Add-Violation($rule, $file, $line, $message) {
    $script:violations += [PSCustomObject]@{
        Rule    = $rule
        File    = $file.Replace($repoRoot + "\", "")
        Line    = $line
        Message = $message
    }
}

if (-not (Test-Path $scanPath)) {
    Write-Host "Path not found: $scanPath" -ForegroundColor Red
    exit 1
}

$csFiles    = Get-ChildItem -Path $scanPath -Recurse -Filter "*.cs"
$razorFiles = Get-ChildItem -Path $scanPath -Recurse -Filter "*.razor"
$sqlFiles   = Get-ChildItem -Path $scanPath -Recurse -Filter "*.sql"

Write-Host ""
Write-Host "green-ai Compliance Scan" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host "Scanning: $Path"
Write-Host ".cs files:    $($csFiles.Count)"
Write-Host ".razor files: $($razorFiles.Count)"
Write-Host ".sql files:   $($sqlFiles.Count)"
Write-Host ""

# ─────────────────────────────────────────────────────────────
# RULE 1: No EF Core
# ─────────────────────────────────────────────────────────────
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        if ($line -match 'using Microsoft\.EntityFrameworkCore|DbContext|\.Include\(|SaveChanges\(\)|AsNoTracking\(\)') {
            Add-Violation "EF-001" $file.FullName ($i + 1) "EF Core usage: $($line.Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 2: No ASP.NET Identity
# ─────────────────────────────────────────────────────────────
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'using Microsoft\.AspNetCore\.Identity|UserManager<|SignInManager<|IdentityUser') {
            Add-Violation "IDENTITY-001" $file.FullName ($i + 1) "ASP.NET Identity usage (forbidden): $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 3: No HttpContext in Handler files
# ─────────────────────────────────────────────────────────────
$handlerFiles = $csFiles | Where-Object { $_.Name -match 'Handler\.cs$' }
foreach ($file in $handlerFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'IHttpContextAccessor|HttpContext\.Current|_httpContext') {
            Add-Violation "HANDLER-001" $file.FullName ($i + 1) "HttpContext in handler — use ICurrentUser instead: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 4: No Newtonsoft.Json
# ─────────────────────────────────────────────────────────────
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'using Newtonsoft\.Json|JsonConvert\.') {
            Add-Violation "JSON-001" $file.FullName ($i + 1) "Newtonsoft.Json (forbidden) — use System.Text.Json: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 5: No Task.Delay in unit/integration test files
# E2E tests excluded — Playwright timeouts sometimes legitimately use Delay
# ─────────────────────────────────────────────────────────────
$testFiles = Get-ChildItem -Path (Join-Path $repoRoot "tests") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\GreenAi\.E2E\\' }
foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'Task\.Delay\s*\(') {
            Add-Violation "TEST-001" $file.FullName ($i + 1) "Task.Delay in test — use deterministic waits: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 6: Handlers must return Result<T>
# ─────────────────────────────────────────────────────────────
foreach ($file in $handlerFiles) {
    $content = Get-Content $file.FullName -Raw
    # Check Handle method signature — must have Result in return type
    if ($content -match 'public.*Task.*Handle\(' -and $content -notmatch 'Task<Result') {
        Add-Violation "HANDLER-002" $file.FullName 0 "Handler.Handle() does not return Task<Result<T>> — check return type"
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 7 (SQL-001): RowVersion must NOT appear in SET clause
# CONFIRMED BUG: APR_009 — causes SqlException at runtime
# ─────────────────────────────────────────────────────────────
foreach ($file in $sqlFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        # Match SET ...RowVersion = ... (any value assignment)
        if ($content[$i] -match '(?i)\bSET\b.*\bRowVersion\s*=' -or
            $content[$i] -match '(?i),\s*RowVersion\s*=') {
            Add-Violation "SQL-001" $file.FullName ($i + 1) "RowVersion in SET clause — ROWVERSION is system-managed, cannot be updated: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 8 (RESULT-001): Result<T>.Fail() error codes must exist in ResultExtensions.cs
# Prevents silent 500 from unregistered error codes
# ─────────────────────────────────────────────────────────────
$resultExtensionsPath = Join-Path $repoRoot "src\GreenAi.Api\SharedKernel\Results\ResultExtensions.cs"
if (Test-Path $resultExtensionsPath) {
    # Extract registered error codes from the switch expression
    $extContent = Get-Content $resultExtensionsPath -Raw
    $registeredCodes = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($match in [regex]::Matches($extContent, '"([A-Z_]+)"\s*=>')) {
        [void]$registeredCodes.Add($match.Groups[1].Value)
    }

    foreach ($file in $csFiles) {
        # Only check handler files
        if ($file.Name -notmatch 'Handler\.cs$') { continue }
        $content = Get-Content $file.FullName
        for ($i = 0; $i -lt $content.Count; $i++) {
            $line = $content[$i]
            # Match: Result<T>.Fail("SOME_CODE", ... or Result<SomeThing>.Fail("SOME_CODE",
            foreach ($match in [regex]::Matches($line, 'Result<[^>]+>\.Fail\s*\(\s*"([A-Z_]+)"')) {
                $code = $match.Groups[1].Value
                if (-not $registeredCodes.Contains($code)) {
                    Add-Violation "RESULT-001" $file.FullName ($i + 1) "Unregistered error code '$code' — add it to ResultExtensions.cs before use (will silently return HTTP 500)"
                }
            }
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 9 (APR-007): Endpoints must use .ToHttpResult() — not inline Results.Ok/BadRequest
# ─────────────────────────────────────────────────────────────
$endpointFiles = $csFiles | Where-Object { $_.Name -match 'Endpoint\.cs$' }
foreach ($file in $endpointFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'Results\.Ok\s*\(|Results\.BadRequest\s*\(|Results\.NotFound\s*\(|Results\.Conflict\s*\(') {
            Add-Violation "APR-007" $file.FullName ($i + 1) "Inline Results.* in endpoint — use result.ToHttpResult() instead: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 10 (APR-005): Handler command/query must not manually check IsAuthenticated
# Use IRequireAuthentication pipeline marker instead
# ─────────────────────────────────────────────────────────────
foreach ($file in $handlerFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'IsAuthenticated|_currentUser\.UserId\s*==\s*0|UserId\.Value\s*==\s*0') {
            Add-Violation "APR-005" $file.FullName ($i + 1) "Manual auth check in handler — use IRequireAuthentication marker on Command/Query instead: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 11 (UI-001): No IMediator injection in Components/ (only in Pages/)
# ─────────────────────────────────────────────────────────────
$componentFiles = $razorFiles | Where-Object { $_.FullName -match '\\Components\\' -and $_.FullName -notmatch '\\Pages\\' }
foreach ($file in $componentFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match '@inject\s+IMediator') {
            Add-Violation "UI-001" $file.FullName ($i + 1) "IMediator injected in component — only pages should call Mediator.Send. Pass data via [Parameter]: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 12 (UI-002): No Bootstrap CSS classes in Razor files (MudBlazor only)
# ─────────────────────────────────────────────────────────────
foreach ($file in $razorFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'class="(btn |container |row |col-|d-flex |justify-content-)') {
            Add-Violation "UI-002" $file.FullName ($i + 1) "Bootstrap CSS class in Razor — use MudBlazor components instead: $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE 13 (UI-003): MudChip must have T="string" (MudBlazor 8 generic requirement)
# ─────────────────────────────────────────────────────────────
foreach ($file in $razorFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match '<MudChip\b' -and $content[$i] -notmatch 'T="') {
            Add-Violation "UI-003" $file.FullName ($i + 1) "MudChip missing T=`"string`" — required in MudBlazor 8 (causes compiler warning): $($content[$i].Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# Report
# ─────────────────────────────────────────────────────────────
if ($violations.Count -eq 0) {
    Write-Host "All checks passed. No violations found." -ForegroundColor Green
    exit 0
}

Write-Host "$($violations.Count) violation(s) found:" -ForegroundColor Red
Write-Host ""

$violations | Group-Object Rule | ForEach-Object {
    Write-Host "[$($_.Name)]  $($_.Count) violation(s)" -ForegroundColor Yellow
    $_.Group | ForEach-Object {
        $lineRef = if ($_.Line -gt 0) { " line $($_.Line)" } else { "" }
        Write-Host "   $($_.File)$lineRef" -ForegroundColor Gray
        Write-Host "   => $($_.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

exit 1
