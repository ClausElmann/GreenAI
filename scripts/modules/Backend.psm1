<#
.SYNOPSIS
    Backend lifecycle management — Level 2 primitives (green-ai)
.DESCRIPTION
    Handles build, start, stop, wait for GreenAi.Api.
    All functions are independently callable.

    Functions:
    - Stop-BackendProcesses   : Kill any running GreenAi.Api processes
    - Clear-Port5057          : Kill process listening on port 5057
    - Build-Backend           : dotnet build GreenAi.Api.csproj
    - Start-BackendAsync      : Start backend as background job
    - Wait-ForBackendReady    : Poll /api/ping until 200 or timeout
    - Stop-Backend            : Stop background job + processes
    - Test-BackendHealth      : Single health check (returns bool)

.NOTES
    Adapted from NeeoBovisWeb/Common-AutonomousTest.psm1
    green-ai specifics: port 5057, http (not https), /api/ping health check
    Dependencies: Core.psm1
#>

$ErrorActionPreference = "Stop"

$script:BackendJob = $null

#region Process Cleanup

function Stop-BackendProcesses {
    [CmdletBinding()]
    param()

    $killed = 0
    # Kill dotnet processes running GreenAi.Api
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)" -ErrorAction SilentlyContinue).CommandLine
            if ($cmd -like "*GreenAi.Api*") {
                Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
                $killed++
            }
        }
        catch {}
    }

    if ($script:BackendJob) {
        Stop-Job -Job $script:BackendJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:BackendJob -Force -ErrorAction SilentlyContinue
        $script:BackendJob = $null
    }

    if ($killed -gt 0) {
        Write-Host "  Stopped $killed backend process(es)" -ForegroundColor DarkGray
    }
}

function Clear-Port5057 {
    [CmdletBinding()]
    param()

    $pids = netstat -ano 2>$null |
            Select-String ":5057\s" |
            ForEach-Object { ($_ -split '\s+')[-1] } |
            Sort-Object -Unique |
            Where-Object { $_ -match '^\d+$' }

    foreach ($p in $pids) {
        try {
            Stop-Process -Id ([int]$p) -Force -ErrorAction SilentlyContinue
            Write-Host "  Cleared port 5057 (PID $p)" -ForegroundColor DarkGray
        }
        catch {}
    }
}

#endregion

#region Build

function Build-Backend {
    [CmdletBinding()]
    param()

    $repoRoot  = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $csproj    = Join-Path $repoRoot "src\GreenAi.Api\GreenAi.Api.csproj"

    Write-Host "Building backend..." -ForegroundColor Yellow
    $output = dotnet build $csproj -v q --nologo 2>&1
    $ok = ($LASTEXITCODE -eq 0)

    if ($ok) {
        Write-Host "  ✓ Build succeeded" -ForegroundColor Green
    }
    else {
        Write-Host "  ✗ Build failed:" -ForegroundColor Red
        $output | Where-Object { $_ -match "error" } | ForEach-Object {
            Write-Host "    $_" -ForegroundColor Red
        }
    }

    return $ok
}

#endregion

#region Start / Stop

function Start-BackendAsync {
    [CmdletBinding()]
    param()

    $repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $csproj   = Join-Path $repoRoot "src\GreenAi.Api\GreenAi.Api.csproj"

    Stop-BackendProcesses
    Clear-Port5057

    Write-Host "Starting backend (async)..." -ForegroundColor Yellow

    $script:BackendJob = Start-Job -ScriptBlock {
        param($proj)
        dotnet run --project $proj --urls "http://localhost:5057" 2>&1
    } -ArgumentList $csproj

    return $script:BackendJob
}

function Stop-Backend {
    [CmdletBinding()]
    param()

    Stop-BackendProcesses
    Write-Host "Backend stopped." -ForegroundColor DarkGray
}

#endregion

#region Health

function Test-BackendHealth {
    [CmdletBinding()]
    param(
        [int]$TimeoutSeconds = 5
    )

    try {
        $response = Invoke-WebRequest `
            -Uri "http://localhost:5057/api/ping" `
            -Method Get `
            -TimeoutSec $TimeoutSeconds `
            -ErrorAction Stop
        return ($response.StatusCode -eq 200)
    }
    catch {
        return $false
    }
}

function Wait-ForBackendReady {
    [CmdletBinding()]
    param(
        [int]$MaxWaitSeconds = 45,
        [int]$PollIntervalSeconds = 2,
        [switch]$Silent
    )

    if (-not $Silent) {
        Write-Host "Waiting for backend (http://localhost:5057/api/ping)..." -ForegroundColor Yellow
        Write-Host "  Max wait: ${MaxWaitSeconds}s" -ForegroundColor DarkGray
    }

    $startTime = Get-Date
    $attempt   = 0

    while (((Get-Date) - $startTime).TotalSeconds -lt $MaxWaitSeconds) {
        $attempt++

        if (Test-BackendHealth -TimeoutSeconds 3) {
            $elapsed = [Math]::Round(((Get-Date) - $startTime).TotalSeconds, 1)
            if (-not $Silent) {
                Write-Host "  ✓ Backend ready in ${elapsed}s (attempt $attempt)" -ForegroundColor Green
            }
            return $true
        }

        if (-not $Silent) {
            $elapsed = [Math]::Round(((Get-Date) - $startTime).TotalSeconds, 0)
            Write-Host "  Attempt $attempt — not ready yet (${elapsed}s)" -ForegroundColor DarkGray
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    }

    $total = [Math]::Round(((Get-Date) - $startTime).TotalSeconds, 1)
    if (-not $Silent) {
        Write-Host "  ✗ Timeout: backend not ready after ${total}s" -ForegroundColor Red
    }
    return $false
}

#endregion

Export-ModuleMember -Function `
    Stop-BackendProcesses, Clear-Port5057, `
    Build-Backend, `
    Start-BackendAsync, Stop-Backend, `
    Test-BackendHealth, Wait-ForBackendReady
