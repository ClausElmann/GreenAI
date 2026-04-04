<#
.SYNOPSIS
    Sync alle labels fra GreenAI LIVE (itgain.dk) til lokal GreenAI_DEV.
.DESCRIPTION
    PROBLEM: Labels oprettes via API mod live. Lokal dev DB (GreenAI_DEV) er forældet
             → UI viser label-nøgler i stedet for tekst under lokal debugging.
    LØSNING: Hent alle labels fra live, MERGE ind i lokal DB.

    Bruger MERGE-pattern = insert nye, opdater eksisterende. Sletter ikke.

.PARAMETER WhatIf
    Preview-tilstand: vis antal labels uden at skrive til DB.
.PARAMETER LanguageId
    Synk kun ét sprog (1=Dansk, 2=English). Default: alle sprog.

.EXAMPLE
    pwsh -File scripts/localization/Sync-Labels.ps1
    pwsh -File scripts/localization/Sync-Labels.ps1 -WhatIf
    pwsh -File scripts/localization/Sync-Labels.ps1 -LanguageId 1

.NOTES
    Credentials: appsettings.Production.json → LabelManagementApi.{Email,Password}
    Kilde: GET https://itgain.dk/api/localization/{languageId}
    Mål:   (localdb)\MSSQLLocalDB / GreenAI_DEV / [dbo].[Labels]
#>

param(
    [switch]$WhatIf,
    [int]$LanguageId = 0   # 0 = alle
)

$ErrorActionPreference = "Stop"

# ── Credentials ──────────────────────────────────────────────────────────────
$repoRoot     = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$prodSettings = Join-Path $repoRoot "src\GreenAi.Api\appsettings.Production.json"

if (-not (Test-Path $prodSettings)) {
    Write-Host "❌ appsettings.Production.json ikke fundet" -ForegroundColor Red; exit 1
}

$cfg      = Get-Content $prodSettings -Raw | ConvertFrom-Json
$email    = $cfg.LabelManagementApi?.Email    ?? $cfg.AdminUser?.Email
$password = $cfg.LabelManagementApi?.Password ?? $cfg.AdminUser?.Password
$baseUrl  = "https://itgain.dk"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Label Sync: Live → GreenAI_DEV$(if ($WhatIf) { ' [WHATIF]' })" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

# ── Login ────────────────────────────────────────────────────────────────────
Write-Host "1. Logger ind ..." -ForegroundColor Gray
$loginBody = @{ Email = $email; Password = $password } | ConvertTo-Json
$login     = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$headers   = @{ "Authorization" = "Bearer $($login.accessToken)" }
Write-Host "   ✅ OK" -ForegroundColor Green

# ── Hent labels fra live ─────────────────────────────────────────────────────
$languageIds = if ($LanguageId -gt 0) { @($LanguageId) } else { @(1, 2) }
$allLabels   = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($lid in $languageIds) {
    Write-Host "2. Henter labels for languageId=$lid ..." -ForegroundColor Gray
    $dict = Invoke-RestMethod -Uri "$baseUrl/api/localization/$lid" -Method GET -Headers $headers
    $count = ($dict.PSObject.Properties | Measure-Object).Count
    Write-Host "   ✅ $count labels hentet" -ForegroundColor Green

    foreach ($prop in $dict.PSObject.Properties) {
        $allLabels.Add([PSCustomObject]@{
            ResourceName  = $prop.Name
            ResourceValue = $prop.Value
            LanguageId    = $lid
        })
    }
}

Write-Host ""
Write-Host "   Total: $($allLabels.Count) labels at synce" -ForegroundColor White

if ($WhatIf) {
    Write-Host ""
    Write-Host "  [WHATIF] Ingen ændringer skrevet til DB." -ForegroundColor Yellow
    exit 0
}

# ── MERGE ind i GreenAI_DEV ──────────────────────────────────────────────────
Write-Host "3. Merger ind i GreenAI_DEV ..." -ForegroundColor Gray

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=GreenAI_DEV;Trusted_Connection=True;TrustServerCertificate=True;"
$upserted = 0
foreach ($label in $allLabels) {
    $sql = @"
MERGE [dbo].[Labels] AS target
USING (SELECT '$($label.ResourceName -replace "'","''")'  AS ResourceName,
              $($label.LanguageId) AS LanguageId) AS source
    ON target.ResourceName = source.ResourceName AND target.LanguageId = source.LanguageId
WHEN MATCHED THEN
    UPDATE SET ResourceValue = N'$($label.ResourceValue -replace "'","''")', UpdatedAt = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
    INSERT (LanguageId, ResourceName, ResourceValue)
    VALUES ($($label.LanguageId), N'$($label.ResourceName -replace "'","''")', N'$($label.ResourceValue -replace "'","''")');
"@
    Invoke-Sqlcmd -ServerInstance "(localdb)\MSSQLLocalDB" -Database "GreenAI_DEV" `
                  -TrustServerCertificate -Query $sql | Out-Null
    $upserted++
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ Sync komplet — $upserted labels merget ind i GreenAI_DEV" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
