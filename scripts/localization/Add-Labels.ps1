<#
.SYNOPSIS
    Opret eller opdater labels i GreenAI LIVE via API (itgain.dk).
.DESCRIPTION
    WORKFLOW:
    1. Login til https://itgain.dk/api/auth/login
    2. POST labels til /api/labels/batch-upsert
    3. Labels er straks aktive i prod — og i dev efter Sync-Labels.ps1

    REGLER:
    - Kør ALTID mod live (itgain.dk) — aldrig localhost
    - Tjek eksisterende labels FØRST (Get-Labels.ps1) — genbrug shared.* labels
    - Credentials læses fra appsettings.Production.json (gitignored)

.PARAMETER Labels
    Array af hashtables: @{ ResourceName="..."; ResourceValue="..."; LanguageId=1 }
    LanguageId: 1=Dansk, 2=English

.EXAMPLE
    # Inline (copy-paste klar):
    $labels = @(
        @{ ResourceName="shared.Save";    ResourceValue="Gem";   LanguageId=1 }
        @{ ResourceName="shared.Save";    ResourceValue="Save";  LanguageId=2 }
        @{ ResourceName="shared.Cancel";  ResourceValue="Annuller"; LanguageId=1 }
        @{ ResourceName="shared.Cancel";  ResourceValue="Cancel";   LanguageId=2 }
    )
    pwsh -File scripts/localization/Add-Labels.ps1 -Labels $labels

.NOTES
    Credentials: appsettings.Production.json → LabelManagementApi.{Email,Password}
    API: POST https://itgain.dk/api/labels/batch-upsert
    Kræver: App kørende på itgain.dk
#>

param(
    [Parameter(Mandatory=$true)]
    [hashtable[]]$Labels
)

$ErrorActionPreference = "Stop"

# ── Credentials fra appsettings.Production.json ─────────────────────────────
$repoRoot     = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$prodSettings = Join-Path $repoRoot "src\GreenAi.Api\appsettings.Production.json"

if (-not (Test-Path $prodSettings)) {
    Write-Host "❌ appsettings.Production.json ikke fundet: $prodSettings" -ForegroundColor Red
    Write-Host "   (gitignored — skal eksistere lokalt)" -ForegroundColor DarkGray
    exit 1
}

$cfg      = Get-Content $prodSettings -Raw | ConvertFrom-Json
$email    = $cfg.LabelManagementApi?.Email
$password = $cfg.LabelManagementApi?.Password

# Fallback til AdminUser hvis LabelManagementApi ikke er sat
if (-not $email)    { $email    = $cfg.AdminUser?.Email }
if (-not $password) { $password = $cfg.AdminUser?.Password }

if (-not $email -or -not $password) {
    Write-Host "❌ Credentials mangler i appsettings.Production.json" -ForegroundColor Red
    Write-Host "   Forventet: LabelManagementApi.Email + LabelManagementApi.Password" -ForegroundColor DarkGray
    exit 1
}

$baseUrl = "https://itgain.dk"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GreenAI Label Management — $baseUrl" -ForegroundColor Cyan
Write-Host "  Labels at oprette/opdatere: $($Labels.Count)" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Login ────────────────────────────────────────────────────────────
Write-Host "1. Logger ind som $email ..." -ForegroundColor Gray
$loginBody = @{ Email = $email; Password = $password } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token   = $login.accessToken
$headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
Write-Host "   ✅ Token modtaget" -ForegroundColor Green

# ── Step 2: Batch upsert ─────────────────────────────────────────────────────
Write-Host "2. Upserter $($Labels.Count) labels ..." -ForegroundColor Gray

$resources = $Labels | ForEach-Object {
    @{
        resourceName  = $_.ResourceName
        resourceValue = $_.ResourceValue
        languageId    = $_.LanguageId
    }
}

$body = @{ labels = $resources } | ConvertTo-Json -Depth 3
$result = Invoke-RestMethod -Uri "$baseUrl/api/labels/batch-upsert" -Method POST -Body $body -Headers $headers

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ Done — $($result.upsertedCount) labels upserted" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "  Næste: Sync til lokal dev DB:" -ForegroundColor Gray
Write-Host "  pwsh -File scripts/localization/Sync-Labels.ps1" -ForegroundColor Yellow
Write-Host ""
