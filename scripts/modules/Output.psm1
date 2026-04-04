<#
.SYNOPSIS
    Formatted console output helpers — Level 1 primitives (green-ai)
.DESCRIPTION
    Consistent colored output for all autonomous scripts.

    Functions:
    - Write-Header         : Section header box
    - Write-Success        : ✓ green message
    - Write-Failure        : ✗ red message
    - Write-Info           : ℹ  cyan message
    - Write-Warning        : ⚠  yellow message
    - Write-Step           : Numbered step (gray)
    - Write-Detail         : Indented detail line (dark gray)
    - Write-Separator      : Horizontal line

.NOTES
    Adapted from NeeoBovisWeb/Output.psm1
    No dependencies.
#>

function Write-Header {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Title)
    $line = "═" * [Math]::Max(50, $Title.Length + 4)
    Write-Host ""
    Write-Host $line -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host $line -ForegroundColor Cyan
}

function Write-Success {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "  ✓ $Message" -ForegroundColor Green
}

function Write-Failure {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "  ✗ $Message" -ForegroundColor Red
}

function Write-Info {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "  ℹ $Message" -ForegroundColor Cyan
}

function Write-Warning {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "  ⚠ $Message" -ForegroundColor Yellow
}

function Write-Step {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][int]$Number,
        [Parameter(Mandatory)][string]$Message
    )
    Write-Host "  [$Number] $Message" -ForegroundColor White
}

function Write-Detail {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "      $Message" -ForegroundColor DarkGray
}

function Write-Separator {
    [CmdletBinding()]
    param([int]$Width = 50)
    Write-Host ("─" * $Width) -ForegroundColor DarkGray
}

Export-ModuleMember -Function `
    Write-Header, Write-Success, Write-Failure, `
    Write-Info, Write-Warning, Write-Step, Write-Detail, Write-Separator
