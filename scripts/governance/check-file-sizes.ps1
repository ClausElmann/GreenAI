<#
.SYNOPSIS
    Check SSOT documentation file sizes for compliance.

.DESCRIPTION
    Validates all .md files under docs/SSOT/ against the 450/600 line thresholds.
    Exits 1 if any CRITICAL violations (>600 lines) are found.

.PARAMETER Area
    Scope the check to a specific SSOT area (backend, database, localization, identity, testing).
    Default: "all"

.PARAMETER Detailed
    Show all files, not just violations.

.EXAMPLE
    .\scripts\governance\check-file-sizes.ps1
    .\scripts\governance\check-file-sizes.ps1 -Area backend -Detailed
#>
param(
    [string]$Area = "all",
    [switch]$Detailed
)

$ErrorActionPreference = "Stop"
$repoRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$ssotRoot = Join-Path $repoRoot "docs\SSOT"

if (-not (Test-Path $ssotRoot)) {
    Write-Host "No docs/SSOT/ folder found. Nothing to check." -ForegroundColor Yellow
    exit 0
}

function Get-SizeCategory($lines) {
    if ($lines -lt 450)  { return @{ Label = "IDEAL";    Color = "Green";  Symbol = "OK" } }
    if ($lines -le 600)  { return @{ Label = "WARNING";  Color = "Yellow"; Symbol = "!!" } }
    return                        @{ Label = "CRITICAL"; Color = "Red";    Symbol = "XX" }
}

$scanRoot = if ($Area -eq "all") {
    $ssotRoot
} else {
    Join-Path $ssotRoot $Area
}

if (-not (Test-Path $scanRoot)) {
    Write-Host "Area not found: $scanRoot" -ForegroundColor Red
    exit 1
}

$files   = Get-ChildItem -Path $scanRoot -Recurse -Filter "*.md"
$results = @()

foreach ($file in $files) {
    $lines    = (Get-Content $file.FullName | Measure-Object -Line).Lines
    $category = Get-SizeCategory $lines
    $rel      = $file.FullName.Replace($repoRoot + "\", "")
    $results += [PSCustomObject]@{
        File     = $rel
        Lines    = $lines
        Label    = $category.Label
        Color    = $category.Color
        Symbol   = $category.Symbol
    }
}

$ideal    = @($results | Where-Object { $_.Label -eq "IDEAL"    }).Count
$warning  = @($results | Where-Object { $_.Label -eq "WARNING"  }).Count
$critical = @($results | Where-Object { $_.Label -eq "CRITICAL" }).Count
$total    = $results.Count

Write-Host ""
Write-Host "SSOT File Size Report — $Area" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

if ($total -eq 0) {
    Write-Host "No .md files found." -ForegroundColor Gray
    exit 0
}

Write-Host "OK   IDEAL    (<450 lines): $ideal / $total" -ForegroundColor Green
Write-Host "!!   WARNING  (450-600):    $warning / $total" -ForegroundColor Yellow
Write-Host "XX   CRITICAL (>600):       $critical / $total" -ForegroundColor $(if ($critical -gt 0) { "Red" } else { "Green" })

if ($critical -gt 0) {
    Write-Host ""
    Write-Host "CRITICAL VIOLATIONS — must split before adding content:" -ForegroundColor Red
    $results | Where-Object { $_.Label -eq "CRITICAL" } | ForEach-Object {
        Write-Host "   XX  [$($_.Lines) lines]  $($_.File)" -ForegroundColor Red
    }
}

if ($Detailed) {
    Write-Host ""
    Write-Host "All files (sorted by size desc):" -ForegroundColor Cyan
    $results | Sort-Object Lines -Descending | ForEach-Object {
        Write-Host "   $($_.Symbol)  [$($_.Lines) lines]  $($_.File)" -ForegroundColor $_.Color
    }
}

Write-Host ""

if ($critical -gt 0) {
    Write-Host "Action required: split files >600 lines before adding new content." -ForegroundColor Red
    exit 1
}

Write-Host "All checks passed." -ForegroundColor Green
exit 0
