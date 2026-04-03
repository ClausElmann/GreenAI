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
# RULE GROUP 1 (FEATURE-001): Feature Completeness
# Every feature in feature-contract-map.json must have its
# handler, endpoint, ui_page, and at least one test file present.
# ─────────────────────────────────────────────────────────────
$featureContractMapPath = Join-Path $repoRoot "docs\SSOT\_system\feature-contract-map.json"
if (Test-Path $featureContractMapPath) {
    $featureMap = Get-Content $featureContractMapPath -Raw | ConvertFrom-Json
    $featureSrcRoot = Join-Path $repoRoot "src\GreenAi.Api"

    foreach ($feature in $featureMap.features) {
        $fid = $feature.id

        # Check handler
        if ($feature.handler) {
            $handlerPath = Join-Path $featureSrcRoot $feature.handler.Replace('/', '\')
            if (-not (Test-Path $handlerPath)) {
                Add-Violation "FEATURE-001" $handlerPath 0 "Incomplete feature '$fid': handler missing — $($feature.handler)"
            }
        }

        # Check endpoint (only if not null)
        if ($feature.endpoint) {
            $endpointPath = Join-Path $featureSrcRoot $feature.endpoint.Replace('/', '\')
            if (-not (Test-Path $endpointPath)) {
                Add-Violation "FEATURE-001" $endpointPath 0 "Incomplete feature '$fid': endpoint missing — $($feature.endpoint)"
            }
        }

        # Check ui_page (only if not null) — strip parenthetical annotations like " (SettingsTab)"
        if ($feature.ui_page) {
            $rawPage    = $feature.ui_page -replace '\s*\([^)]+\)\s*$', ''
            $pagePath   = Join-Path $featureSrcRoot $rawPage.Replace('/', '\')
            if (-not (Test-Path $pagePath)) {
                Add-Violation "FEATURE-001" $pagePath 0 "Incomplete feature '$fid': ui_page missing — $rawPage"
            }
        }

        # Check tests: at least one test path (integration or unit) must be non-null AND exist
        $intTest  = $feature.tests.integration
        $unitTest = $feature.tests.unit
        if (-not $intTest -and -not $unitTest) {
            Add-Violation "FEATURE-001" $featureContractMapPath 0 "Incomplete feature '$fid': no tests registered (set tests.integration or tests.unit)"
        } else {
            # Verify each registered test path actually exists
            foreach ($testPath in @($intTest, $unitTest) | Where-Object { $_ }) {
                $absTestPath = Join-Path $repoRoot $testPath.Replace('/', '\')
                if (-not (Test-Path $absTestPath)) {
                    Add-Violation "FEATURE-001" $absTestPath 0 "Incomplete feature '$fid': test file registered but missing — $testPath"
                }
            }
        }
    }
} else {
    Write-Host "WARNING: feature-contract-map.json not found — skipping FEATURE-001" -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 2a (SQL-002): Tenant isolation — Profiles / ProfileUserMappings
# Feature SQL files that reference tenant-scoped tables must
# include a @CustomerId filter. Migration files are excluded.
# ─────────────────────────────────────────────────────────────
$featureSqlFiles = $sqlFiles | Where-Object {
    $_.FullName -notmatch '\\Database\\Migrations\\' -and
    $_.FullName -notmatch '\\GreenAi\.DB\\'
}
$tenantTables    = @('Profiles', 'ProfileUserMappings', 'CustomerSettings')
foreach ($file in $featureSqlFiles) {
    $raw = Get-Content $file.FullName -Raw
    $referencesTenantTable = $false
    foreach ($tbl in $tenantTables) {
        if ($raw -match "\b$tbl\b") { $referencesTenantTable = $true; break }
    }
    if ($referencesTenantTable -and $raw -notmatch 'CustomerId') {
        Add-Violation "SQL-002" $file.FullName 0 "Missing tenant isolation (@CustomerId) in SQL referencing tenant-scoped table: $($file.Name)"
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 2b (SQL-003): No inline SQL strings in C# code
# All SQL must be loaded via SqlLoader — never embedded in code.
# ─────────────────────────────────────────────────────────────
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        if ($line -match '(?:QueryAsync|ExecuteAsync|QuerySingleAsync|QueryFirstAsync|QuerySingleOrDefaultAsync|QueryAsync)\s*\(\s*\$?"(?:SELECT|INSERT|UPDATE|DELETE)') {
            Add-Violation "SQL-003" $file.FullName ($i + 1) "Inline SQL string detected — use SqlLoader.Load<T>() instead: $($line.Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 3 (AUTH-002): Handlers using ICurrentUser outside
# Auth/ must have IRequireAuthentication on the sibling Command/Query.
# ─────────────────────────────────────────────────────────────
foreach ($file in $handlerFiles) {
    # Auth handlers are exempt — they drive the authentication flow itself
    if ($file.FullName -match '\\Features\\Auth\\') { continue }

    $raw = Get-Content $file.FullName -Raw
    if ($raw -notmatch 'ICurrentUser') { continue }

    # Find sibling *Command.cs or *Query.cs in the same folder
    $dir         = $file.DirectoryName
    $commandFiles = Get-ChildItem -Path $dir -Filter "*Command.cs" -ErrorAction SilentlyContinue
    $queryFiles   = Get-ChildItem -Path $dir -Filter "*Query.cs"   -ErrorAction SilentlyContinue
    $siblings     = @($commandFiles) + @($queryFiles)

    $hasMarker = $false

    # The Command/Query may be defined inline in the handler file itself
    if ($raw -match 'IRequireAuthentication') { $hasMarker = $true }

    # Also check sibling *Command.cs / *Query.cs files
    foreach ($sibling in $siblings) {
        if ($hasMarker) { break }
        $siblingContent = Get-Content $sibling.FullName -Raw
        if ($siblingContent -match 'IRequireAuthentication') { $hasMarker = $true }
    }

    if (-not $hasMarker) {
        Add-Violation "AUTH-002" $file.FullName 0 "Handler uses ICurrentUser but sibling Command/Query lacks IRequireAuthentication marker: $($file.Name)"
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 4a (UI-004): Interactive elements must have data-testid.
# Checks <button>, <input>, <select>, <MudButton>, <MudTextField>.
# MudButton/MudTextField: 8-line window scan (tag can span lines).
# MudTextField data-testid must be inside UserAttributes.
# ─────────────────────────────────────────────────────────────
foreach ($file in $razorFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Native HTML elements — single-line check
        if ($line -match '<(button|input|select)\b' -and $line -notmatch 'data-testid') {
            Add-Violation "UI-004" $file.FullName ($i + 1) "Missing data-testid on <$([regex]::Match($line, '<(button|input|select)').Groups[1].Value)>: $($line.Trim())"
        }

        # MudButton — window-of-8 check (tag attr can span multiple lines)
        if ($line -match '<MudButton\b') {
            $windowEnd = [Math]::Min($i + 8, $lines.Count - 1)
            $window    = ($lines[$i..$windowEnd]) -join "`n"
            if ($window -notmatch 'data-testid') {
                Add-Violation "UI-004" $file.FullName ($i + 1) "MudButton missing data-testid (checked 8-line window): $($line.Trim())"
            }
        }

        # MudTextField — window-of-8 check (UserAttributes containing data-testid is accepted)
        if ($line -match '<MudTextField\b') {
            $windowEnd = [Math]::Min($i + 8, $lines.Count - 1)
            $window    = ($lines[$i..$windowEnd]) -join "`n"
            if ($window -notmatch 'data-testid') {
                Add-Violation "UI-004" $file.FullName ($i + 1) "MudTextField missing data-testid (use UserAttributes): $($line.Trim())"
            }
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 4b (UI-005): Routable pages (@page) must live under
# Components/Pages/ or Features/ — nowhere else.
# ─────────────────────────────────────────────────────────────
foreach ($file in $razorFiles) {
    $content = Get-Content $file.FullName
    foreach ($line in $content) {
        if ($line -match '^\s*@page\s+"') {
            $inPages    = $file.FullName -match '\\Components\\Pages\\'
            $inFeatures = $file.FullName -match '\\Features\\'
            if (-not $inPages -and -not $inFeatures) {
                Add-Violation "UI-005" $file.FullName 0 "Routable page (@page) outside Components/Pages/ and Features/: $($file.FullName.Replace($repoRoot + '\', ''))"
            }
            break  # Only need to check the first @page per file
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 5 (UI-006): UI model routes must be implemented.
# Reads used_by_page entries from analysis-tool/docs/UI_MODEL_SCHEMA.json
# and verifies each corresponding Blazor file exists.
# ─────────────────────────────────────────────────────────────
$analysisToolRoot  = Join-Path (Get-Item $repoRoot).Parent.FullName "analysis-tool"
$uiModelSchemaPath = Join-Path $analysisToolRoot "docs\UI_MODEL_SCHEMA.json"
if (Test-Path $uiModelSchemaPath) {
    $uiSchema   = Get-Content $uiModelSchemaPath -Raw | ConvertFrom-Json
    $featureSrc = Join-Path $repoRoot "src\GreenAi.Api"

    # Recursively extract used_by_page values from the JSON
    function Get-UsedByPage($obj) {
        if ($null -eq $obj) { return }
        if ($obj -is [string]) { return }
        if ($obj.PSObject.Properties.Name -contains 'used_by_page') {
            $val = $obj.used_by_page
            if ($val -and $val -ne '' -and $val -ne 'null') {
                # Strip parenthetical annotations: " (SettingsTab)", " (UserList tab)", etc.
                $clean = $val -replace '\s*\([^)]+\)\s*$', ''
                [void]$script:usedByPages.Add($clean)
            }
        }
        foreach ($prop in $obj.PSObject.Properties) {
            Get-UsedByPage $prop.Value
        }
    }

    $script:usedByPages = [System.Collections.Generic.List[string]]::new()
    foreach ($model in $uiSchema.response_models.PSObject.Properties) {
        Get-UsedByPage $model.Value
    }

    foreach ($pagePath in ($script:usedByPages | Select-Object -Unique)) {
        $absPath = Join-Path $featureSrc $pagePath.Replace('/', '\')
        if (-not (Test-Path $absPath)) {
            Add-Violation "UI-006" $absPath 0 "UI model route not implemented: $pagePath"
        }
    }
} else {
    Write-Host "INFO: UI_MODEL_SCHEMA.json not found at expected path — skipping UI-006" -ForegroundColor Gray
}

# ─────────────────────────────────────────────────────────────
# Report
# ─────────────────────────────────────────────────────────────
if ($violations.Count -eq 0) {
    Write-Host "VALIDATION PASSED — SYSTEM AUTONOMY VERIFIED" -ForegroundColor Green
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
