<#
.SYNOPSIS
    Core PowerShell utilities — Level 1 primitives (green-ai)
.DESCRIPTION
    Foundation layer. NO dependencies on other modules.

    Functions:
    - Get-Timestamp          : Standardized timestamp string
    - Get-AppSettings        : Read appsettings.Development.json with cache
    - Get-ConnectionString   : Return GreenAI_DEV connection string
    - Get-BaseUrl            : Return portal base URL
    - Get-Credentials        : Return dev credentials from appsettings

.NOTES
    Adapted from NeeoBovisWeb/docs/SSOT/powershell/modules/Core.psm1
    green-ai specifics: port 5057, GreenAI_DEV, appsettings.Development.json
#>

#region Timestamps
function Get-Timestamp {
    param([string]$Format = "yyyy-MM-dd HH:mm:ss")
    return (Get-Date).ToString($Format)
}
#endregion

#region Configuration

$script:AppSettingsCache = $null

function Get-AppSettings {
    [CmdletBinding()]
    param([switch]$NoCache)

    if ($script:AppSettingsCache -and -not $NoCache) {
        return $script:AppSettingsCache
    }

    # Search order: appsettings.Development.json → appsettings.json
    $repoRoot   = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $devConfig  = Join-Path $repoRoot "src\GreenAi.Api\appsettings.Development.json"
    $baseConfig = Join-Path $repoRoot "src\GreenAi.Api\appsettings.json"

    $path = if (Test-Path $devConfig) { $devConfig } elseif (Test-Path $baseConfig) { $baseConfig } else {
        throw "appsettings.json not found. Looked in: $devConfig"
    }

    $script:AppSettingsCache = Get-Content $path -Raw | ConvertFrom-Json
    return $script:AppSettingsCache
}

function Get-ConnectionString {
    # Always returns Dev connection string (GreenAI_DEV on localdb)
    return "Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Trusted_Connection=True;TrustServerCertificate=True;"
}

function Get-BaseUrl {
    return "http://localhost:5057"
}

function Get-Credentials {
    # Dev seed data from V015_SeedDevData.sql
    return @{
        Email    = "admin@dev.local"
        Password = "dev123"
    }
}

#endregion

Export-ModuleMember -Function Get-Timestamp, Get-AppSettings, Get-ConnectionString, Get-BaseUrl, Get-Credentials
