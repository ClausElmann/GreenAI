<#
.SYNOPSIS
    Database utilities — Level 2 primitives (green-ai)
.DESCRIPTION
    Runs SQL against GreenAI_DEV on (localdb)\MSSQLLocalDB via sqlcmd.

    Functions:
    - Test-DatabaseConnection  : Quick connection test (returns bool)
    - Invoke-DatabaseQuery     : Run SQL, return rows as PSObjects
    - Invoke-DatabaseNonQuery  : Run INSERT/UPDATE/DELETE (returns rows affected)

.NOTES
    Adapted from NeeoBovisWeb/Database.psm1
    Uses sqlcmd CLI — no Invoke-Sqlcmd dependency.
    Dependencies: Core.psm1
#>

$ErrorActionPreference = "Stop"

#region Connection

function Test-DatabaseConnection {
    [CmdletBinding()]
    param()

    try {
        $result = sqlcmd `
            -S "(localdb)\MSSQLLocalDB" `
            -d "GreenAI_DEV" `
            -Q "SELECT 1 AS ping" `
            -h -1 -s "|" -W 2>&1

        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
}

#endregion

#region Query

function Invoke-DatabaseQuery {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Sql,
        [string]$Database = "GreenAI_DEV"
    )

    # Write SQL to temp file (handles newlines + special chars)
    $tmpFile = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        Set-Content -Path $tmpFile -Value $Sql -Encoding UTF8

        $raw = sqlcmd `
            -S "(localdb)\MSSQLLocalDB" `
            -d $Database `
            -i $tmpFile `
            -h 1 -s "|" -W 2>&1

        if ($LASTEXITCODE -ne 0) {
            $err = ($raw | Where-Object { $_ -match "Msg\s+\d+|Error" }) -join "`n"
            throw "SQL error (exit $LASTEXITCODE): $err"
        }

        # Parse pipe-delimited output → PSObjects
        $lines = $raw | Where-Object { $_ -and $_ -notmatch "^\s*$" -and $_ -notmatch "^\s*-+\s*$" }
        if (-not $lines -or $lines.Count -lt 2) { return @() }

        $headers = ($lines[0] -split '\|') | ForEach-Object { $_.Trim() }
        $rows = @()
        foreach ($line in ($lines | Select-Object -Skip 1)) {
            if ($line -match "\(\d+ rows? affected\)") { continue }
            $vals = ($line -split '\|') | ForEach-Object { $_.Trim() }
            $obj  = [PSCustomObject]@{}
            for ($i = 0; $i -lt $headers.Count; $i++) {
                $obj | Add-Member -MemberType NoteProperty -Name $headers[$i] -Value ($vals[$i] ?? "")
            }
            $rows += $obj
        }
        return $rows
    }
    finally {
        Remove-Item $tmpFile -ErrorAction SilentlyContinue
    }
}

function Invoke-DatabaseNonQuery {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Sql,
        [string]$Database = "GreenAI_DEV"
    )

    $tmpFile = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        Set-Content -Path $tmpFile -Value $Sql -Encoding UTF8

        $raw = sqlcmd `
            -S "(localdb)\MSSQLLocalDB" `
            -d $Database `
            -i $tmpFile `
            -h -1 -s "|" -W 2>&1

        if ($LASTEXITCODE -ne 0) {
            $err = ($raw | Where-Object { $_ -match "Msg\s+\d+|Error" }) -join "`n"
            throw "SQL error (exit $LASTEXITCODE): $err"
        }

        # Extract "X rows affected" count
        $affected = $raw | Select-String "\((\d+) rows? affected\)" | ForEach-Object {
            [int]$_.Matches[0].Groups[1].Value
        } | Select-Object -Last 1

        return ($affected ?? 0)
    }
    finally {
        Remove-Item $tmpFile -ErrorAction SilentlyContinue
    }
}

#endregion

Export-ModuleMember -Function Test-DatabaseConnection, Invoke-DatabaseQuery, Invoke-DatabaseNonQuery
