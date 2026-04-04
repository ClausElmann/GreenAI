<#
.SYNOPSIS
    Vis eksisterende labels fra GreenAI LIVE — DRY-check inden nye labels oprettes.
.PARAMETER Filter
    Tekst-filter på ResourceName (wildcard, f.eks. "shared.*")
.PARAMETER LanguageId
    1=Dansk (default), 2=English
.EXAMPLE
    pwsh -File scripts/localization/Get-Labels.ps1
    pwsh -File scripts/localization/Get-Labels.ps1 -Filter "shared.*"
    pwsh -File scripts/localization/Get-Labels.ps1 -Filter "farm.*" -LanguageId 2
#>

param(
    [string]$Filter = "*",
    [int]$LanguageId = 1
)

$ErrorActionPreference = "Stop"

$repoRoot     = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$prodSettings = Join-Path $repoRoot "src\GreenAi.Api\appsettings.Production.json"
$cfg          = Get-Content $prodSettings -Raw | ConvertFrom-Json
$email        = $cfg.LabelManagementApi?.Email    ?? $cfg.AdminUser?.Email
$password     = $cfg.LabelManagementApi?.Password ?? $cfg.AdminUser?.Password
$baseUrl      = "https://itgain.dk"

$loginBody = @{ Email = $email; Password = $password } | ConvertTo-Json
$login     = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$headers   = @{ "Authorization" = "Bearer $($login.accessToken)" }

$dict  = Invoke-RestMethod -Uri "$baseUrl/api/localization/$LanguageId" -Method GET -Headers $headers
$lang  = if ($LanguageId -eq 1) { "Dansk" } else { "English" }

$keys = $dict.PSObject.Properties.Name | Where-Object { $_ -like $Filter } | Sort-Object

Write-Host ""
Write-Host "  [$lang] Labels matching '$Filter' ($($keys.Count) results):" -ForegroundColor Cyan
Write-Host ""

foreach ($key in $keys) {
    Write-Host "  $($key.PadRight(50)) $($dict.$key)" -ForegroundColor Gray
}
Write-Host ""
