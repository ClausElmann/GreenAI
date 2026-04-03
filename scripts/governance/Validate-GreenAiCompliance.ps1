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
        # Scan all C# files under Features/ — not just handlers
        if ($file.FullName -notmatch '\\Features\\') { continue }
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
# RULE GROUP 6 (ID-001): Strongly-typed IDs — no raw int for
# UserId, CustomerId, ProfileId in Command/Query records.
# Exception: HTTP boundary types (API token command) where the
# int comes from JSON deserialization and is wrapped in handler.
# ─────────────────────────────────────────────────────────────
$commandQueryFiles = $csFiles | Where-Object { $_.Name -match '(Command|Query)\.cs$' }
foreach ($file in $commandQueryFiles) {
    $content = Get-Content $file.FullName -Raw
    # Detect raw int UserId / CustomerId / ProfileId in record parameter lists
    if ($content -match 'public\s+(?:sealed\s+)?record\s+\w+\s*\([^)]*\bint\s+(UserId|CustomerId|ProfileId)\b') {
        # Allow HTTP boundary files: Auth/ commands receive int from JSON binding (handler wraps in typed ID)
        # Allow Api/V1/ commands that also receive int from JSON
        if ($file.FullName -notmatch '\\Features\\Auth\\' -and $file.FullName -notmatch '\\Api\\V1\\') {
            Add-Violation "ID-001" $file.FullName 0 "Raw int for identity value in $($file.Name) — use UserId/CustomerId/ProfileId strongly-typed struct (SharedKernel/Ids/StrongIds.cs)"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 7 (LOC-001): No hardcoded strings in Blazor files.
# Strings in markup or code blocks must use @Loc.Get("key").
# Exemptions: CSS class names, attribute values (Accept, type=...),
#   href values, log messages, comments, framework-generated files.
# ─────────────────────────────────────────────────────────────
$exemptRazorFiles = @("Error.razor", "ReconnectModal.razor", "_Imports.razor",
                      "App.razor", "Routes.razor", "NavMenu.razor",
                      "NotFound.razor",    # system page — not localised
                      "MainLayout.razor")  # brand name "GreenAi" + system reconnect UI intentionally exempt
$locRazorFiles = $razorFiles | Where-Object {
    $exemptRazorFiles -notcontains $_.Name -and
    $_.FullName -notmatch '\\Components\\Pages\\Error\.razor'
}
foreach ($file in $locRazorFiles) {
    $content = Get-Content $file.FullName
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]

        # Skip blank lines
        if ([string]::IsNullOrWhiteSpace($line)) { continue }

        # Skip Razor comment lines and C# comment lines
        if ($line -match '^\s*@\*' -or $line -match '^\s*//') { continue }

        # Skip @code block lines (C# inside @code { } — not markup text nodes)
        # (These are harder to scope perfectly; we skip lines that are pure C# statements)
        if ($line -match '^\s*(private|protected|public|var |await |if |else|foreach|return |throw |bool |int |string |Task)') { continue }

        # Skip attribute-only lines and structural Razor directives
        if ($line -match '^\s*(@page|@using|@inject|@model|@namespace|@inherits|@implements|@layout|@rendermode|@attribute)') { continue }
        if ($line -match '^\s*(class=|id=|href=|src=|data-testid=|style=|type=|name=|value=|placeholder=|autocomplete=)') { continue }

        # ── STRUCTURAL DETECTION: text node between > and < ──────────────────────────
        # Flag any >TEXT< where TEXT is at least 3 word chars and not wrapped in @Loc.Get or @(...)
        # This catches ALL visible UI text regardless of language.
        if ($line -match '>\s*[A-Za-zÆØÅæøå][A-Za-zÆØÅæøå0-9 ,.\-]{2,}[A-Za-zÆØÅæøå0-9]\s*<' -and
            $line -notmatch 'Loc\.Get\(' -and
            $line -notmatch '@\([^)]+\)' -and
            $line -notmatch '^\s*@\*' -and
            $line -notmatch 'data-testid') {
            Add-Violation "LOC-001" $file.FullName ($i + 1) "Hardcoded visible text in markup — use @Loc.Get(`"key`") instead: $($line.Trim())"
        }

        # ── STRUCTURAL DETECTION: C# string literal with visible text ────────────────
        # Catches Title="Some Text", Label="Some Text" etc. in component attributes.
        # Uses -cmatch (case-sensitive) so 'Label' only matches PascalCase Blazor attributes,
        # NOT lowercase HTML attributes like aria-label.
        # Excludes: route segments (/path), CSS classes, test IDs, log messages, navigateTo paths
        if ($line -cmatch '(?:Title|Label|Text|Placeholder|HelperText|ErrorText|ButtonText|NoRecordsContent)\s*=\s*"[A-Za-zÆØÅæøå][^"]{2,}"' -and
            $line -notmatch 'Loc\.Get\(' -and
            $line -notmatch '^\s*//' -and
            $line -notmatch 'data-testid') {
            Add-Violation "LOC-001" $file.FullName ($i + 1) "Hardcoded component text attribute — use @Loc.Get(`"key`") instead: $($line.Trim())"
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 8 (FEATURE-002): sql_files in feature-contract-map.json
# must match files that actually exist on disk.
# ─────────────────────────────────────────────────────────────
if (Test-Path $featureContractMapPath) {
    $featureMap2 = Get-Content $featureContractMapPath -Raw | ConvertFrom-Json
    $featureSrcRoot2 = Join-Path $repoRoot "src\GreenAi.Api"

    foreach ($feature in $featureMap2.features) {
        foreach ($sqlPath in $feature.sql_files) {
            if ($sqlPath) {
                $absPath = Join-Path $featureSrcRoot2 $sqlPath.Replace('/', '\')
                if (-not (Test-Path $absPath)) {
                    Add-Violation "FEATURE-002" $absPath 0 "sql_files entry '$sqlPath' for feature '$($feature.id)' does not exist on disk — update feature-contract-map.json"
                }
            }
        }
    }
}


# ─────────────────────────────────────────────────────────────
# RULE GROUP 9 (SLICE-001): Every Handler.cs under Features/ must be
# registered in feature-contract-map.json. Unregistered features
# bypass the entire audit system.
# ─────────────────────────────────────────────────────────────
if (Test-Path $featureContractMapPath) {
    $featureMapSlice = Get-Content $featureContractMapPath -Raw | ConvertFrom-Json
    $featureSrcRoot3 = Join-Path $repoRoot "src\GreenAi.Api"

    # Build set of registered handler paths (normalised to backslash)
    $registeredHandlers = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($feature in $featureMapSlice.features) {
        if ($feature.handler) {
            [void]$registeredHandlers.Add($feature.handler.Replace('/', '\'))
        }
    }

    # Scan all *Handler.cs files under src/GreenAi.Api/Features/
    $featuresRoot = Join-Path $featureSrcRoot3 "Features"
    if (Test-Path $featuresRoot) {
        Get-ChildItem $featuresRoot -Recurse -Filter "*Handler.cs" | ForEach-Object {
            # Derive relative path from src/GreenAi.Api (e.g. "Features\Auth\Login\LoginHandler.cs")
            $relPath = $_.FullName.Substring($featureSrcRoot3.Length).TrimStart('\')
            $normalised = $relPath.Replace('\', '\')
            if (-not $registeredHandlers.Contains($normalised)) {
                Add-Violation "SLICE-001" $_.FullName 0 "Handler '$($_.Name)' is not registered in feature-contract-map.json — add entry to maintain full governance coverage"
            }
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 10b (FEATURE-003): Blazor-only features (endpoint: null)
# must have tests.e2e registered. Without E2E, Blazor features have
# no automated runtime test — integration tests only cover API layer.
# ─────────────────────────────────────────────────────────────
if (Test-Path $featureContractMapPath) {
    $featureMapF3 = Get-Content $featureContractMapPath -Raw | ConvertFrom-Json

    foreach ($feature in $featureMapF3.features) {
        $fid = $feature.id

        # Only applies to Blazor-only features (no HTTP endpoint)
        if ($null -ne $feature.endpoint) { continue }

        # Must have tests.e2e
        $e2eValue = $feature.tests.e2e
        if (-not $e2eValue) {
            Add-Violation "FEATURE-003" $featureContractMapPath 0 "Blazor-only feature '$fid' has no e2e test registered (tests.e2e is null) — add E2E test path or create E2E test"
        } else {
            # Verify the path exists
            $absE2ePath = Join-Path $repoRoot $e2eValue.Replace('/', '\')
            if (-not (Test-Path $absE2ePath)) {
                Add-Violation "FEATURE-003" $absE2ePath 0 "Feature '$fid': e2e test path registered but file missing — $e2eValue"
            }
        }
    }
}

# ─────────────────────────────────────────────────────────────
# RULE GROUP 11 (EXEC-001): EXECUTION_MEMORY.md must have an entry
# dated within the last 48 hours. Warns when self-improvement loop
# is stale — does NOT count as a violation (advisory).
# ─────────────────────────────────────────────────────────────
$execMemoryPath = Join-Path $repoRoot "docs\SSOT\governance\EXECUTION_MEMORY.md"
if (Test-Path $execMemoryPath) {
    $execContent = Get-Content $execMemoryPath -Raw
    # Extract all "    date: YYYY-MM-DD" entries from the log section
    $datMatches = [regex]::Matches($execContent, '^\s{4}date:\s+(\d{4}-\d{2}-\d{2})', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if ($datMatches.Count -gt 0) {
        $lastDate = $datMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object | Select-Object -Last 1
        try {
            $lastParsed = [datetime]::ParseExact($lastDate, "yyyy-MM-dd", $null)
            $ageHours   = ([datetime]::UtcNow - $lastParsed).TotalHours
            if ($ageHours -gt 48) {
                Write-Host "WARN [EXEC-001] EXECUTION_MEMORY.md last entry is $([math]::Round($ageHours, 0))h old (last: $lastDate). Update after completing work session." -ForegroundColor Yellow
            }
        } catch {
            Write-Host "WARN [EXEC-001] Could not parse date '$lastDate' in EXECUTION_MEMORY.md" -ForegroundColor Yellow
        }
    } else {
        Write-Host "WARN [EXEC-001] No date entries found in EXECUTION_MEMORY.md log section" -ForegroundColor Yellow
    }
}


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
