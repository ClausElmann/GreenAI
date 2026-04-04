#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starter GreenAI portalen og åbner den i Chrome.
.DESCRIPTION
    1. Stopper eventuelle kørende dotnet-processer på port 5057
    2. Bygger projektet
    3. Starter Blazor Server i baggrunden
    4. Venter på at serveren svarer
    5. Åbner http://localhost:5057 i Chrome
.EXAMPLE
    .\start-portal.ps1
#>

$ErrorActionPreference = 'Stop'
$ProjectPath = "$PSScriptRoot\src\GreenAi.Api\GreenAi.Api.csproj"
$Url         = "http://localhost:5057"
$Port        = 5057

Write-Host ""
Write-Host "=== GreenAI Portal Start ===" -ForegroundColor Cyan

# 1. Frigør port
Write-Host "[1/4] Frigør port $Port..." -ForegroundColor Gray
$proc = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -ErrorAction SilentlyContinue
if ($proc) {
    Stop-Process -Id $proc -Force -ErrorAction SilentlyContinue
    Write-Host "      Stoppede proces $proc" -ForegroundColor DarkYellow
} else {
    Write-Host "      Port ledig" -ForegroundColor DarkGray
}

# 2. Byg
Write-Host "[2/4] Bygger..." -ForegroundColor Gray
$build = dotnet build $ProjectPath -v q --nologo 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "" 
    Write-Host "BUILD FEJLEDE:" -ForegroundColor Red
    $build | Where-Object { $_ -match "error" } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
Write-Host "      OK" -ForegroundColor Green

# 3. Start server i et nyt PowerShell-vindue (synligt, kan lukkes manuelt)
Write-Host "[3/4] Starter portal i nyt vindue..." -ForegroundColor Gray
$startArgs = "-NoExit -Command `"cd '$PSScriptRoot'; dotnet run --project '$ProjectPath' --no-build`""
Start-Process "pwsh.exe" -ArgumentList $startArgs

# 4. Vent på HTTP 200
Write-Host "[4/4] Venter på $Url..." -ForegroundColor Gray
$maxWait  = 30   # sekunder
$elapsed  = 0
$ready    = $false
while ($elapsed -lt $maxWait) {
    Start-Sleep -Seconds 1
    $elapsed++
    try {
        $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -lt 500) {
            $ready = $true
            break
        }
    } catch { }
    Write-Host "      ...venter ($elapsed/$maxWait sek)" -ForegroundColor DarkGray
}

if (-not $ready) {
    Write-Host ""
    Write-Host "Server svarede ikke inden $maxWait sekunder." -ForegroundColor Red
    Write-Host "Tjek om der er fejl med: Receive-Job -Id $($job.Id)" -ForegroundColor Yellow
    exit 1
}

# 5. Åbn Chrome
Write-Host ""
Write-Host "✓ Portal klar! Åbner $Url i Chrome..." -ForegroundColor Green
$chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
if (Test-Path $chrome) {
    Start-Process $chrome $Url
} else {
    Start-Process $Url  # fallback: standard-browser
}

Write-Host ""
Write-Host "Portalen kører i det nye pwsh-vindue. Luk det for at stoppe serveren." -ForegroundColor Cyan
Write-Host ""
