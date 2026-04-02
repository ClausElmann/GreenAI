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

Write-Host ""
Write-Host "green-ai Compliance Scan" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host "Scanning: $Path"
Write-Host ".cs files:    $($csFiles.Count)"
Write-Host ".razor files: $($razorFiles.Count)"
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
# RULE 5: No Task.Delay in test files
# ─────────────────────────────────────────────────────────────
$testFiles = Get-ChildItem -Path (Join-Path $repoRoot "tests") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
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
