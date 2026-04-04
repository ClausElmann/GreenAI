#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Restore production backup to LocalDB development database (GreenAI_DEV).

.DESCRIPTION
    Restores a production backup (.bak file) to GreenAI_DEV on LocalDB.
    This lets you develop and test against real production schema without
    touching the production database.

.PARAMETER BackupPath
    Path to the .bak file.
    Default: searches %USERPROFILE%\Downloads for the latest greenai_prod*.bak file.

.PARAMETER Force
    Skip the confirmation prompt.

.EXAMPLE
    .\scripts\database\Restore-ProductionBackup.ps1
    # Auto-finds latest greenai_prod*.bak in Downloads and restores.

.EXAMPLE
    .\scripts\database\Restore-ProductionBackup.ps1 -BackupPath "C:\Backups\greenai_prod_2026-04-04.bak"
    # Restores a specific backup file.

.NOTES
    Prerequisites:
    - SQL Server LocalDB installed (sqllocaldb.exe available)
    - MSSQLLocalDB instance running  (sqllocaldb start MSSQLLocalDB)

    What this script does:
    1. Finds the backup file (or uses the specified path)
    2. Verifies LocalDB is running (starts it if needed)
    3. Drops the existing GreenAI_DEV database (asks for confirmation)
    4. Restores the .bak to GreenAI_DEV
    5. Verifies the table count
    6. Re-runs DbUp migrations (so dev-only schema stays current)
    7. Runs E2E database fixture to re-seed dev users/profiles
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$BackupPath,

    [Parameter(Mandatory = $false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$DbName   = "GreenAI_DEV"
$Server   = "(localdb)\MSSQLLocalDB"
$MdfPath  = "C:\Temp\GreenAI_DEV.mdf"
$LdfPath  = "C:\Temp\GreenAI_DEV_log.ldf"

Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "  RESTORE PRODUCTION BACKUP → $DbName (LocalDB)" -ForegroundColor Green
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Find backup file ─────────────────────────────────────────────────
if (-not $BackupPath) {
    Write-Host "Searching for latest backup in Downloads..." -ForegroundColor Yellow
    $downloads = [Environment]::GetFolderPath("UserProfile") + "\Downloads"
    $files = Get-ChildItem -Path $downloads -Filter "greenai_prod*.bak" -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending

    if ($files) {
        $BackupPath = $files[0].FullName
        Write-Host "  Found : $($files[0].Name)" -ForegroundColor Green
        Write-Host "  Date  : $($files[0].LastWriteTime)" -ForegroundColor DarkGray
        Write-Host "  Size  : $([math]::Round($files[0].Length / 1MB, 2)) MB" -ForegroundColor DarkGray
    }
    else {
        Write-Host "  No greenai_prod*.bak files found in Downloads." -ForegroundColor Red
        Write-Host "  Download a backup from production and save it to:" -ForegroundColor Yellow
        Write-Host "  $downloads\greenai_prod_YYYY-MM-DD.bak" -ForegroundColor Yellow
        exit 1
    }
}

if (-not (Test-Path $BackupPath)) {
    Write-Host "Backup file not found: $BackupPath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ── Step 2: Verify LocalDB ───────────────────────────────────────────────────
Write-Host "Checking SQL Server LocalDB..." -ForegroundColor Yellow

$localDbVersions = sqllocaldb versions 2>$null
if (-not $localDbVersions) {
    Write-Host "  SQL Server LocalDB is not installed." -ForegroundColor Red
    Write-Host "  Download from: https://go.microsoft.com/fwlink/?linkid=2215158" -ForegroundColor Yellow
    exit 1
}

$instanceInfo = sqllocaldb info MSSQLLocalDB 2>$null
if (-not $instanceInfo) {
    Write-Host "  Creating MSSQLLocalDB instance..." -ForegroundColor Yellow
    sqllocaldb create MSSQLLocalDB -s | Out-Null
    Write-Host "  Instance created and started." -ForegroundColor Green
}
else {
    $state = (sqllocaldb info MSSQLLocalDB | Select-String "State:").ToString().Split(":")[1].Trim()
    if ($state -ne "Running") {
        Write-Host "  Starting MSSQLLocalDB..." -ForegroundColor Yellow
        sqllocaldb start MSSQLLocalDB | Out-Null
    }
    Write-Host "  MSSQLLocalDB is running." -ForegroundColor Green
}

$versionLine = (sqllocaldb info MSSQLLocalDB | Select-String "Version:").ToString()
Write-Host "  $versionLine" -ForegroundColor DarkGray
Write-Host ""

# ── Step 3: Confirmation ─────────────────────────────────────────────────────
if (-not $Force) {
    Write-Host "WARNING: This will DROP and REPLACE the $DbName database!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Local database : $DbName on $Server" -ForegroundColor White
    Write-Host "  Backup file    : $BackupPath" -ForegroundColor White
    Write-Host "  Data file      : $MdfPath" -ForegroundColor White
    Write-Host "  Log file       : $LdfPath" -ForegroundColor White
    Write-Host ""
    $confirm = Read-Host "Continue? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Host "Cancelled." -ForegroundColor Red
        exit 0
    }
    Write-Host ""
}

# ── Step 4: Drop existing database ──────────────────────────────────────────
Write-Host "Dropping $DbName (if exists)..." -ForegroundColor Yellow

$dropSql = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$DbName')
BEGIN
    ALTER DATABASE [$DbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DbName];
    PRINT 'Dropped.'
END
ELSE
    PRINT 'Did not exist.'
"@

sqlcmd -S $Server -Q $dropSql -b 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
if ($LASTEXITCODE -ne 0) { Write-Host "  Could not drop (may be OK if it did not exist)." -ForegroundColor Yellow }
else { Write-Host "  Done." -ForegroundColor Green }
Write-Host ""

# ── Step 5: Restore backup ───────────────────────────────────────────────────
Write-Host "Restoring backup (may take 1–2 minutes)..." -ForegroundColor Yellow

if (-not (Test-Path "C:\Temp")) { New-Item -Path "C:\Temp" -ItemType Directory | Out-Null }

# Peek inside the BAK to get the logical file names
$fileListSql = "RESTORE FILELISTONLY FROM DISK = N'$BackupPath'"
$fileListRaw = sqlcmd -S $Server -Q $fileListSql -h -1 -W 2>&1
# Parse first two non-empty data rows for logical names
$logicalNames = $fileListRaw | Where-Object { $_ -match "^\S" -and $_ -notmatch "^-" -and $_ -notmatch "LogicalName" } |
                Select-Object -First 2 |
                ForEach-Object { ($_ -split '\s+')[0] }
$logicalData = if ($logicalNames.Count -ge 1) { $logicalNames[0] } else { "GreenAI_DEV" }
$logicalLog  = if ($logicalNames.Count -ge 2) { $logicalNames[1] } else { "GreenAI_DEV_log" }

$restoreSql = @"
RESTORE DATABASE [$DbName]
FROM DISK = N'$BackupPath'
WITH
    MOVE '$logicalData' TO '$MdfPath',
    MOVE '$logicalLog'  TO '$LdfPath',
    REPLACE,
    STATS = 10;
"@

sqlcmd -S $Server -Q $restoreSql -b 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Restore FAILED. Check the output above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  Backup restored." -ForegroundColor Green
Write-Host ""

# ── Step 6: Verify ───────────────────────────────────────────────────────────
Write-Host "Verifying database..." -ForegroundColor Yellow

$stateRaw  = sqlcmd -S $Server -d master -Q "SELECT state_desc FROM sys.databases WHERE name = '$DbName'" -h -1 -W
if ($stateRaw -match "ONLINE") {
    Write-Host "  State  : ONLINE" -ForegroundColor Green
}
else {
    Write-Host "  Database is NOT online after restore. Aborting." -ForegroundColor Red
    exit 1
}

$countRaw   = sqlcmd -S $Server -d $DbName -Q "SELECT COUNT(*) FROM sys.tables" -h -1 -W
$tableCount = [int]($countRaw.Trim())
Write-Host "  Tables : $tableCount" -ForegroundColor Green

if ($tableCount -lt 10) {
    Write-Host "  WARNING: Expected at least 10 tables — found $tableCount. Backup may be incomplete." -ForegroundColor Yellow
}

Write-Host ""

# ── Step 7: Re-run DbUp migrations ──────────────────────────────────────────
Write-Host "Re-running DbUp migrations (applies dev-only schema on top of prod)..." -ForegroundColor Yellow
$buildResult = dotnet run --project src/GreenAi.Api/GreenAi.Api.csproj -- migrate-only 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  Migrations applied." -ForegroundColor Green
}
else {
    Write-Host "  Migration output:" -ForegroundColor DarkGray
    $buildResult | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    Write-Host ""
    Write-Host "  WARNING: Migrations may not have applied cleanly. Check output above." -ForegroundColor Yellow
}

Write-Host ""

# ── Done ─────────────────────────────────────────────────────────────────────
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "  DONE — $DbName is ready" -ForegroundColor Green
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor White
Write-Host "    dotnet test tests/GreenAi.Tests -v q            # integration tests" -ForegroundColor DarkGray
Write-Host "    dotnet run --project src/GreenAi.Api/...        # start app" -ForegroundColor DarkGray
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""
