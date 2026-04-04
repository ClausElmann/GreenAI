<#
.SYNOPSIS
    HTTP operations — Level 2 primitives (green-ai)
.DESCRIPTION
    Authenticated HTTP calls against GreenAi.Api (http://localhost:5057).

    Functions:
    - Get-AuthToken      : POST /api/auth/login → JWT token string
    - Get-AuthHeaders    : Return @{ Authorization = "Bearer <token>" }
    - Invoke-GetRequest  : GET with optional auth
    - Invoke-PostRequest : POST JSON body with optional auth
    - Invoke-PutRequest  : PUT JSON body with optional auth
    - Invoke-DeleteRequest : DELETE with optional auth
    - Invoke-HttpRequest : Generic — all methods

    Return shape (all functions):
      @{ Success=$true; StatusCode=200; Body=<parsed>; Raw=<string>; Error="" }

.NOTES
    Adapted from NeeoBovisWeb/Http.psm1
    Dependencies: Core.psm1
#>

$ErrorActionPreference = "Stop"

$script:CachedToken = $null

#region Auth

function Get-AuthToken {
    [CmdletBinding()]
    param(
        [string]$Email    = "",
        [string]$Password = ""
    )

    $modulesDir = $PSScriptRoot
    Import-Module (Join-Path $modulesDir "Core.psm1") -Force

    if (-not $Email -or -not $Password) {
        $creds    = Get-Credentials
        $Email    = $creds.Email
        $Password = $creds.Password
    }

    $body = @{ email = $Email; password = $Password } | ConvertTo-Json

    try {
        $response = Invoke-WebRequest `
            -Uri "http://localhost:5057/api/auth/login" `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -ErrorAction Stop

        $parsed = $response.Content | ConvertFrom-Json
        # Token may be at $parsed.token or $parsed.accessToken — try both
        $token = $parsed.token ?? $parsed.accessToken ?? $parsed.data?.token ?? $parsed.data?.accessToken
        if (-not $token) {
            throw "Could not find token field in login response: $($response.Content)"
        }
        $script:CachedToken = $token
        return $token
    }
    catch {
        throw "Login failed: $_"
    }
}

function Get-AuthHeaders {
    [CmdletBinding()]
    param([string]$Token = "")

    if (-not $Token) {
        if ($script:CachedToken) {
            $Token = $script:CachedToken
        }
        else {
            $Token = Get-AuthToken
        }
    }
    return @{ Authorization = "Bearer $Token" }
}

#endregion

#region Core HTTP

function Invoke-HttpRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Uri,
        [hashtable]$Headers     = @{},
        [object]$Body           = $null,
        [int]$TimeoutSeconds    = 30,
        [switch]$NoAuth
    )

    if (-not $Uri.StartsWith("http")) {
        $Uri = "http://localhost:5057$Uri"
    }

    if (-not $NoAuth -and -not $Headers.ContainsKey("Authorization")) {
        try { $Headers += Get-AuthHeaders } catch {}
    }

    $params = @{
        Uri        = $Uri
        Method     = $Method
        Headers    = $Headers
        TimeoutSec = $TimeoutSeconds
        ErrorAction = "Stop"
    }

    if ($null -ne $Body) {
        $params.Body        = ($Body | ConvertTo-Json -Depth 10)
        $params.ContentType = "application/json"
    }

    try {
        $response = Invoke-WebRequest @params
        $parsed = $null
        try { $parsed = $response.Content | ConvertFrom-Json } catch {}
        return @{
            Success    = $true
            StatusCode = [int]$response.StatusCode
            Body       = $parsed
            Raw        = $response.Content
            Error      = ""
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        $rawBody = ""
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $rawBody = $reader.ReadToEnd()
        }
        catch {}
        return @{
            Success    = $false
            StatusCode = $statusCode
            Body       = $null
            Raw        = $rawBody
            Error      = $_.Exception.Message
        }
    }
}

function Invoke-GetRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Uri,
        [hashtable]$Headers  = @{},
        [switch]$NoAuth
    )
    return Invoke-HttpRequest -Method "GET" -Uri $Uri -Headers $Headers -NoAuth:$NoAuth
}

function Invoke-PostRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Uri,
        [object]$Body        = $null,
        [hashtable]$Headers  = @{},
        [switch]$NoAuth
    )
    return Invoke-HttpRequest -Method "POST" -Uri $Uri -Body $Body -Headers $Headers -NoAuth:$NoAuth
}

function Invoke-PutRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Uri,
        [object]$Body        = $null,
        [hashtable]$Headers  = @{},
        [switch]$NoAuth
    )
    return Invoke-HttpRequest -Method "PUT" -Uri $Uri -Body $Body -Headers $Headers -NoAuth:$NoAuth
}

function Invoke-DeleteRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Uri,
        [hashtable]$Headers  = @{},
        [switch]$NoAuth
    )
    return Invoke-HttpRequest -Method "DELETE" -Uri $Uri -Headers $Headers -NoAuth:$NoAuth
}

#endregion

Export-ModuleMember -Function `
    Get-AuthToken, Get-AuthHeaders, `
    Invoke-HttpRequest, `
    Invoke-GetRequest, Invoke-PostRequest, Invoke-PutRequest, Invoke-DeleteRequest
