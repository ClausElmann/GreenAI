<#
.SYNOPSIS
    Query [dbo].[Logs] in GreenAI_DEV for recent errors/warnings.
.DESCRIPTION
    Replaces reading backend console output. Used during test debugging to see
    what the backend logged without needing a running process.
.PARAMETER LastMinutes
    How many minutes back to query (default: 10)
.PARAMETER Level
    Filter: All | Warning | Error (default: Error)
.EXAMPLE
    pwsh -File scripts/testing/Get-Latest-Errors.ps1
    pwsh -File scripts/testing/Get-Latest-Errors.ps1 -LastMinutes 30 -Level Warning
.NOTES
    Logs table columns: Id, Message, Level, TimeStamp, Exception, SourceContext, TraceId
#>

param(
    [int]$LastMinutes = 10,

    [ValidateSet("All", "Warning", "Error")]
    [string]$Level = "Error"
)

$modulesDir = Join-Path $PSScriptRoot "..\modules"
Import-Module (Join-Path $modulesDir "Core.psm1") -Force

$connectionString = Get-ConnectionString

$levelFilter = switch ($Level) {
    "All"     { "" }
    "Warning" { "AND [Level] IN ('Warning', 'Error', 'Fatal')" }
    "Error"   { "AND [Level] IN ('Error', 'Fatal')" }
}

$query = @"
SELECT TOP 50
    [Id],
    [TimeStamp],
    [Level],
    LEFT([Message], 500)     AS [Message],
    LEFT([Exception], 1000)  AS [Exception],
    [SourceContext],
    [TraceId]
FROM [dbo].[Logs]
WHERE [TimeStamp] > DATEADD(MINUTE, -$LastMinutes, SYSDATETIMEOFFSET())
$levelFilter
ORDER BY [TimeStamp] DESC;
"@

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GreenAI_DEV — [Logs] — last $LastMinutes min — Level: $Level" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

try {
    $rows = Invoke-Sqlcmd `
        -ServerInstance "(localdb)\MSSQLLocalDB" `
        -Database "GreenAI_DEV" `
        -TrustServerCertificate `
        -Query $query `
        -ErrorAction Stop

    if (-not $rows) {
        Write-Host "✅ No $Level entries in the last $LastMinutes minutes." -ForegroundColor Green
        return
    }

    foreach ($row in $rows) {
        $color = switch ($row.Level) {
            "Fatal"   { "Magenta" }
            "Error"   { "Red" }
            "Warning" { "Yellow" }
            default   { "Gray" }
        }

        $ts = $row.TimeStamp.ToString("HH:mm:ss")
        Write-Host "[$ts] [$($row.Level.PadRight(7))] $($row.Message)" -ForegroundColor $color

        if ($row.Exception -and $row.Exception.Trim()) {
            Write-Host "  Exception: $($row.Exception.Substring(0, [Math]::Min(300, $row.Exception.Length)))" -ForegroundColor DarkRed
        }
        if ($row.SourceContext) {
            Write-Host "  Source: $($row.SourceContext)" -ForegroundColor DarkGray
        }
        Write-Host ""
    }

    Write-Host "  Total: $($rows.Count) row(s)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Query failed: $_" -ForegroundColor Red
    Write-Host "   Connection: $connectionString" -ForegroundColor DarkGray
}
