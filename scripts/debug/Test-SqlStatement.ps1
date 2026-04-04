<#
.SYNOPSIS
    SQL pre-validation tool — validate SQL against GreenAI_DEV before running tests.
.DESCRIPTION
    ⚡ AI AUTONOMOUS TOOL: When a test/handler fails with SQL error →
      Extract SQL from handler's .sql file → Run this script → Get EXACT error in 5 seconds.
    10x faster than trial-and-error via integration tests.

.PARAMETER SqlStatement
    SQL to validate. Can include @Parameters.
    Example: "SELECT * FROM Users WHERE Id = @Id"

.PARAMETER Parameters
    Hashtable of @{ ParameterName = SampleValue }.
    String values get default 'test', int values get default 1.
    Example: @{ Id = 1; Email = "test@test.com" }

.PARAMETER SqlFile
    Path to .sql file instead of inline SqlStatement.
    Example: "src/GreenAi.Api/Features/Auth/Login/Login.sql"

.PARAMETER ShowDetails
    Show full SQL with parameter declarations.

.PARAMETER Database
    Target database (default: GreenAI_DEV)

.EXAMPLE
    # Validate inline SQL
    pwsh -File scripts/debug/Test-SqlStatement.ps1 `
        -SqlStatement "SELECT * FROM Users WHERE Email = @Email" `
        -Parameters @{ Email = "test@test.com" }

    # Validate from .sql file
    pwsh -File scripts/debug/Test-SqlStatement.ps1 `
        -SqlFile "src/GreenAi.Api/Features/Auth/Login/Login.sql" `
        -ShowDetails

    # Validate UPDATE with params
    pwsh -File scripts/debug/Test-SqlStatement.ps1 `
        -SqlStatement "UPDATE Users SET Email = @Email WHERE Id = @Id AND CustomerId = @CustomerId" `
        -Parameters @{ Email = "x@y.com"; Id = 1; CustomerId = 1 }

.NOTES
    SSOT: docs/SSOT/testing/sql-validation.md
    Adapted from NeeoBovisWeb/scripts/database/Test-SqlStatement.ps1
#>

param(
    [string]$SqlStatement = "",
    [hashtable]$Parameters = @{},
    [string]$SqlFile       = "",
    [switch]$ShowDetails,
    [string]$Database = "GreenAI_DEV"
)

$ErrorActionPreference = "Stop"

# ─── Resolve SQL ──────────────────────────────────────────────────────────────

if ($SqlFile) {
    if (-not [System.IO.Path]::IsPathRooted($SqlFile)) {
        $repoRoot  = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
        $SqlFile   = Join-Path $repoRoot $SqlFile
    }
    if (-not (Test-Path $SqlFile)) {
        Write-Host "❌ File not found: $SqlFile" -ForegroundColor Red
        exit 1
    }
    $SqlStatement = Get-Content $SqlFile -Raw
}

if (-not $SqlStatement) {
    Write-Host "❌ Provide -SqlStatement or -SqlFile" -ForegroundColor Red
    exit 1
}

# ─── Build parameterized SQL ──────────────────────────────────────────────────

$declares = ""
if ($Parameters.Count -gt 0) {
    $declLines = @()
    foreach ($key in $Parameters.Keys) {
        $val = $Parameters[$key]
        switch ($val.GetType().Name) {
            { $_ -in "Int32","Int64","Int16" } {
                $declLines += "DECLARE @$key INT = $val;"
            }
            "Boolean" {
                $declLines += "DECLARE @$key BIT = $(if ($val) { 1 } else { 0 });"
            }
            "DateTime" {
                $declLines += "DECLARE @$key DATETIME2 = '$($val.ToString("yyyy-MM-dd HH:mm:ss"))';"
            }
            "Decimal" {
                $declLines += "DECLARE @$key DECIMAL(18,4) = $val;"
            }
            default {
                $escaped = "$val".Replace("'","''")
                $declLines += "DECLARE @$key NVARCHAR(500) = '$escaped';"
            }
        }
    }
    $declares = ($declLines -join "`n") + "`n"
}

$fullSql = $declares + $SqlStatement

# ─── Header ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SQL VALIDATOR — GreenAI_DEV" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($ShowDetails -or $Parameters.Count -gt 0) {
    Write-Host ""
    Write-Host "  Parameters:" -ForegroundColor White
    if ($Parameters.Count -eq 0) {
        Write-Host "    (none)" -ForegroundColor DarkGray
    }
    else {
        foreach ($k in $Parameters.Keys) {
            Write-Host "    @$k = $($Parameters[$k])" -ForegroundColor DarkGray
        }
    }
}

if ($ShowDetails) {
    Write-Host ""
    Write-Host "  SQL:" -ForegroundColor White
    $SqlStatement -split "`n" | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
}

Write-Host ""
Write-Host "  Executing..." -ForegroundColor Yellow

# ─── Execute via sqlcmd ───────────────────────────────────────────────────────

$tmpFile = [System.IO.Path]::GetTempFileName() + ".sql"
try {
    Set-Content -Path $tmpFile -Value $fullSql -Encoding UTF8

    $raw = sqlcmd `
        -S "(localdb)\MSSQLLocalDB" `
        -d $Database `
        -i $tmpFile `
        -h 1 -s "|" -W 2>&1

    $exitCode = $LASTEXITCODE
}
finally {
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}

# ─── Result ───────────────────────────────────────────────────────────────────

if ($exitCode -eq 0) {
    Write-Host "  ✓ SQL VALIDATION PASSED" -ForegroundColor Green
    Write-Host ""

    # Show rows if SELECT
    $dataLines = $raw | Where-Object {
        $_ -and $_ -notmatch "^\s*$" -and $_ -notmatch "^\s*-+\|" -and $_ -notmatch "\(\d+ rows? affected\)"
    }
    if ($dataLines.Count -gt 0) {
        Write-Host "  Result rows:" -ForegroundColor White
        $dataLines | Select-Object -First 20 | ForEach-Object {
            Write-Host "    $_" -ForegroundColor DarkGray
        }
    }

    Write-Host ""
    exit 0
}

# ─── Error diagnosis ──────────────────────────────────────────────────────────

Write-Host "  ✗ SQL VALIDATION FAILED" -ForegroundColor Red
Write-Host ""

$errorLines = $raw | Where-Object { $_ -match "Msg\s+\d+|Error|error" }
$errorText  = ($raw | Where-Object { $_ }) -join "`n"

Write-Host "  Raw error:" -ForegroundColor Yellow
$errorLines | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
Write-Host ""

# Pattern matching → diagnosis + fix options
$diagnosis = ""
$fixOptions = @()

switch -Regex ($errorText) {
    "Invalid column name '(.+?)'" {
        $col = $Matches[1]
        $diagnosis = "Column '$col' does not exist in the table."
        $fixOptions = @(
            "Check column name: grep_search '$col' in the relevant .sql schema file",
            "Fix typo in SQL statement (e.g. CreatedAt vs CreatedUtc)",
            "Add missing column via DbUp migration if this is a new requirement"
        )
        break
    }
    "Cannot insert the value NULL into column '(.+?)'" {
        $col = $Matches[1]
        $diagnosis = "Column '$col' is NOT NULL but no value was provided."
        $fixOptions = @(
            "Add @$col to the SQL parameters",
            "Add DEFAULT value in schema (if NULL is not a valid state)",
            "Pass a value in the handler/command"
        )
        break
    }
    "Invalid object name '(.+?)'" {
        $tbl = $Matches[1]
        $diagnosis = "Table '$tbl' does not exist."
        $fixOptions = @(
            "Check table name spelling (case-insensitive but typo-sensitive)",
            "Verify migration ran: SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES",
            "Check schema prefix (e.g. dbo.$tbl)"
        )
        break
    }
    "conflicted with the CHECK constraint '(.+?)'" {
        $c = $Matches[1]
        $diagnosis = "Value violates CHECK constraint '$c'."
        $fixOptions = @(
            "Read constraint definition in schema file to see allowed values",
            "Change parameter value to satisfy the constraint",
            "Review if constraint is correct for the business rule"
        )
        break
    }
    "conflicted with the FOREIGN KEY constraint '(.+?)'" {
        $c = $Matches[1]
        $diagnosis = "Referenced row does not exist (FK constraint '$c')."
        $fixOptions = @(
            "Insert the parent record first",
            "Use an existing ID: SELECT TOP 1 Id FROM <table>",
            "Check test seed data creates required parent records"
        )
        break
    }
    "Violation of UNIQUE KEY constraint '(.+?)'" {
        $c = $Matches[1]
        $diagnosis = "Duplicate value violates UNIQUE constraint '$c'."
        $fixOptions = @(
            "Use a different unique value in the test",
            "Add IF NOT EXISTS guard in SQL",
            "Use MERGE (UPSERT) if record may already exist"
        )
        break
    }
    "String or binary data would be truncated" {
        $diagnosis = "String value exceeds column size."
        $fixOptions = @(
            "Shorten the string value to fit the column",
            "Increase column size in schema migration",
            "Add MaxLength validation in FluentValidator"
        )
        break
    }
    "Incorrect syntax near '(.+?)'" {
        $near = $Matches[1]
        $diagnosis = "SQL syntax error near '$near'."
        $fixOptions = @(
            "Review SQL for missing comma, unclosed quote, or bracket",
            "Check @parameter names match between DECLARE and usage",
            "Validate SQL directly in Azure Data Studio"
        )
        break
    }
    "Conversion failed when converting" {
        $diagnosis = "Type mismatch — value cannot be converted to the target type."
        $fixOptions = @(
            "Check that C# type maps to SQL type (int → INT, string → NVARCHAR)",
            "Ensure no numeric ID is passed as string",
            "Add explicit CAST/CONVERT in SQL if type coercion is needed"
        )
        break
    }
    default {
        $diagnosis = "Unclassified SQL error — read raw output above."
        $fixOptions = @(
            "Search Msg number in SQL Server docs",
            "Run query in Azure Data Studio for richer formatting"
        )
    }
}

Write-Host "  Diagnosis:" -ForegroundColor Yellow
Write-Host "    $diagnosis" -ForegroundColor White
Write-Host ""
Write-Host "  Fix options:" -ForegroundColor Yellow
for ($i = 0; $i -lt $fixOptions.Count; $i++) {
    Write-Host "    Option $($i+1): $($fixOptions[$i])" -ForegroundColor White
}
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

exit 1
