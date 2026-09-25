<#
    ---------------------------------------------------------------------------
    File        : configure-server.ps1
    Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
    Author      : NIMSARA R V P P (IT 23215306)
    Created     : 2026-09-20
    Description : Copies the secret settings from your local development
                  configuration into the IIS server's configuration, then
                  recycles the application pool so the change takes effect.

                  Run this ONCE after the first deployment. The server file
                  then survives every later run of deploy.ps1, so you do not
                  need to run this again unless the secrets change.

    Why a script rather than editing the file by hand
                  appsettings.Production.json lives under C:\inetpub, which is
                  protected, so editing it needs an elevated editor. Copying the
                  values across also avoids a mistyped connection string, which
                  is the most common cause of a 500.30 error.

    Note on practice
                  Sharing one secret between development and the server is fine
                  for a single machine student project, and it means one admin
                  password to remember during the demonstration. A real
                  deployment would use a different signing key per environment,
                  so that a leaked development key cannot be used against the
                  live system.

    Usage       : Open PowerShell AS ADMINISTRATOR, then:
                     cd "C:\SLIIT LIFE\SLIIT\Y4S2\EAD\Assignment\Assignment_1\smart-solar-microgrid"
                     .\configure-server.ps1
    ---------------------------------------------------------------------------
#>

[CmdletBinding()]
param(
    [string] $SitePath    = "C:\inetpub\SolarApi",
    [string] $AppPoolName = "SolarMicrogridApi",
    [int]    $Port        = 8081
)

$ErrorActionPreference = "Stop"

$devSettings  = Join-Path $PSScriptRoot "backend\appsettings.Development.json"
$prodSettings = Join-Path $SitePath "appsettings.Production.json"

function Write-Step { param([string] $Text) Write-Host "`n>> $Text" -ForegroundColor Cyan }
function Write-Ok   { param([string] $Text) Write-Host "   OK  $Text" -ForegroundColor Green }
function Write-Bad  { param([string] $Text) Write-Host "   !!  $Text" -ForegroundColor Red }

# --- Must be elevated --------------------------------------------------------
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Bad "Run this from an ADMINISTRATOR PowerShell window."
    exit 1
}

if (-not (Test-Path $devSettings))  { Write-Bad "Not found: $devSettings";  exit 1 }
if (-not (Test-Path $prodSettings)) { Write-Bad "Not found: $prodSettings. Run deploy.ps1 first."; exit 1 }

Write-Step "Copying settings into the server configuration"

$dev  = Get-Content $devSettings  -Raw | ConvertFrom-Json
$prod = Get-Content $prodSettings -Raw | ConvertFrom-Json

# --- Database ----------------------------------------------------------------
$prod.MongoDb.ConnectionString = $dev.MongoDb.ConnectionString
$prod.MongoDb.DatabaseName     = $dev.MongoDb.DatabaseName
Write-Ok "MongoDB connection string"

# --- Token signing key -------------------------------------------------------
$prod.Jwt.SecretKey = $dev.Jwt.SecretKey
$prod.Jwt.Issuer    = $dev.Jwt.Issuer
$prod.Jwt.Audience  = $dev.Jwt.Audience
Write-Ok "JWT signing key"

# --- Bootstrap administrator -------------------------------------------------
# Kept identical to the local one so the same sign in works on both.
$prod.Seed.Nic      = $dev.Seed.Nic
$prod.Seed.FullName = $dev.Seed.FullName
$prod.Seed.Email    = $dev.Seed.Email
$prod.Seed.Password = $dev.Seed.Password
Write-Ok "Bootstrap administrator account"

# --- Save --------------------------------------------------------------------
# Depth 10 because the settings are nested several levels; the default of 2
# would silently flatten the deeper values into the literal text "System.Object".
$prod | ConvertTo-Json -Depth 10 | Set-Content -Path $prodSettings -Encoding utf8
Write-Ok "Saved $prodSettings"

# --- Restart the application so it re-reads the configuration ----------------
# Settings validated at startup are not re-evaluated while the app is running,
# so the pool has to be recycled for the new values to be picked up.
Write-Step "Recycling the application pool"
$appcmd = Join-Path $env:SystemRoot "system32\inetsrv\appcmd.exe"
if (Test-Path $appcmd) {
    & $appcmd recycle apppool /apppool.name:"$AppPoolName" | Out-Null
    Write-Ok "Recycled $AppPoolName"
}
else {
    Write-Bad "appcmd.exe not found; recycle the pool manually in IIS Manager."
}

# --- Verify ------------------------------------------------------------------
Write-Step "Checking http://localhost:$Port/api/health"
$healthy = $false
for ($attempt = 1; $attempt -le 12; $attempt++) {
    Start-Sleep -Seconds 2
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -TimeoutSec 5 -ErrorAction Stop
        Write-Host ""
        $response | ConvertTo-Json
        $healthy = $true
        break
    }
    catch { $lastError = $_.Exception.Message }
}

Write-Host ""
if ($healthy) {
    Write-Host "SERVER CONFIGURED. Your API is live on IIS." -ForegroundColor Green
    Write-Host "  This PC    : http://localhost:$Port/swagger"
    Write-Host "  Your phone : http://192.168.0.2:$Port/swagger  (after the firewall step)"
}
else {
    Write-Host "Still not answering on port $Port." -ForegroundColor Yellow
    Write-Host "  Last error : $lastError"
    Write-Host "  Read the real reason with:"
    Write-Host '    Get-WinEvent -FilterHashtable @{LogName="Application"; ProviderName="IIS AspNetCore Module V2"} -MaxEvents 1 | Select-Object -ExpandProperty Message'
}
