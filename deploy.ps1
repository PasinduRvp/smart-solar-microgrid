<#
    ---------------------------------------------------------------------------
    File        : deploy.ps1
    Project     : Smart Solar Microgrid Trading System - SE4040 Assignment 1
    Author      : NIMSARA R V P P (IT 23215306)
    Created     : 2026-09-20
    Description : Publishes the Web API to the IIS site folder in one command.

                  Run this every time you want IIS to pick up code changes.
                  Day to day development still uses "dotnet run"; this script is
                  for putting a new version onto the hosted server.

    Why the app_offline step
                  IIS keeps the application's files locked while it is running,
                  so copying over them fails with "the process cannot access the
                  file". Dropping a file named app_offline.htm into the site
                  folder tells ASP.NET Core to shut the application down and
                  release those files. Deleting the file starts it again. This
                  is the supported way to update a running site.

    Usage       : Open PowerShell AS ADMINISTRATOR, then:
                     cd "C:\SLIIT LIFE\SLIIT\Y4S2\EAD\Assignment\Assignment_1\smart-solar-microgrid"
                     .\deploy.ps1
    ---------------------------------------------------------------------------
#>

[CmdletBinding()]
param(
    # Folder IIS serves the application from.
    [string] $SitePath = "C:\inetpub\SolarApi",

    # Port the IIS website listens on.
    #
    # 8081 rather than the more usual 8080: on this machine the Oracle TNS
    # Listener (TNSLSNR) already holds 8080, and two programs cannot listen on
    # the same port. Port 80 is taken by the IIS Default Web Site.
    # If this port is ever taken too, pass another one: .\deploy.ps1 -Port 8085
    [int] $Port = 8081
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "backend"
$offlineFile = Join-Path $SitePath "app_offline.htm"
$prodSettings = Join-Path $SitePath "appsettings.Production.json"

function Write-Step { param([string] $Text) Write-Host "`n>> $Text" -ForegroundColor Cyan }
function Write-Ok   { param([string] $Text) Write-Host "   OK  $Text" -ForegroundColor Green }
function Write-Warn { param([string] $Text) Write-Host "   !!  $Text" -ForegroundColor Yellow }

# --- 1. Must be elevated, because C:\inetpub is a protected location ---------
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "This script must be run from an ADMINISTRATOR PowerShell window." -ForegroundColor Red
    Write-Host "Close this window, right-click PowerShell, choose 'Run as administrator', and try again."
    exit 1
}

# --- 2. Locate the .NET SDK -------------------------------------------------
$env:Path = [Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
            [Environment]::GetEnvironmentVariable("Path", "User")

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet was not found on PATH. Is the .NET 8 SDK installed?" -ForegroundColor Red
    exit 1
}

Write-Step "Deploying to $SitePath"

# --- 3. Take the application offline so its files unlock --------------------
if (Test-Path $SitePath) {
    New-Item -Path $offlineFile -ItemType File -Force | Out-Null
    Write-Ok "Application taken offline"

    # A moment for IIS to notice the file and release its handles.
    Start-Sleep -Seconds 2
}
else {
    New-Item -Path $SitePath -ItemType Directory -Force | Out-Null
    Write-Ok "Created $SitePath"
}

# --- 4. Build and copy the published output ---------------------------------
Write-Step "Publishing (Release build)"
Push-Location $projectPath
try {
    dotnet publish -c Release -o $SitePath --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    Write-Ok "Published"
}
finally {
    Pop-Location
}

# --- 5. Make sure the server has its own configuration -----------------------
# appsettings.Production.json holds the Mongo connection string and the JWT
# signing key for the hosted site. It is deliberately NOT in source control and
# NOT produced by the publish, so it is created here as a template once and then
# survives every later deployment.
if (-not (Test-Path $prodSettings)) {
    $template = @'
{
  "//": "SERVER CONFIG - never committed to Git. Fill in the two values below.",
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "MongoDb": {
    "ConnectionString": "PASTE_YOUR_ATLAS_CONNECTION_STRING_HERE",
    "DatabaseName": "SmartSolarMicrogrid",
    "ServerSelectionTimeoutSeconds": 15
  },
  "Jwt": {
    "SecretKey": "PASTE_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS",
    "Issuer": "SolarMicrogrid.Api",
    "Audience": "SolarMicrogrid.Clients",
    "ExpiryMinutes": 120
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "Swagger": { "Enabled": true },
  "Seed": {
    "CreateDefaultBackofficeUser": true,
    "Nic": "199012345678",
    "FullName": "System Administrator",
    "Email": "admin@microgrid.lk",
    "Password": "PASTE_THE_SAME_ADMIN_PASSWORD",
    "AllowDemoDataEndpoint": true
  }
}
'@
    Set-Content -Path $prodSettings -Value $template -Encoding utf8
    Write-Warn "Created a TEMPLATE at $prodSettings"
    Write-Warn "Open it and paste your real values before the site will work."
}
else {
    Write-Ok "Server configuration already present"
}

# --- 6. Bring the application back online ------------------------------------
if (Test-Path $offlineFile) {
    Remove-Item $offlineFile -Force
    Write-Ok "Application back online"
}

# --- 7. Check it actually answers --------------------------------------------
Write-Step "Checking http://localhost:$Port/api/health"
$healthy = $false
for ($attempt = 1; $attempt -le 10; $attempt++) {
    Start-Sleep -Seconds 2
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -TimeoutSec 5 -ErrorAction Stop
        Write-Host ""
        $response | ConvertTo-Json
        $healthy = $true
        break
    }
    catch {
        # The first request wakes the application up, so a few failures here are
        # expected before it answers.
        $lastError = $_.Exception.Message
    }
}

Write-Host ""
if ($healthy) {
    Write-Host "DEPLOYED. The API is live on IIS." -ForegroundColor Green
    Write-Host "  This PC    : http://localhost:$Port/swagger"
    Write-Host "  Your phone : http://192.168.0.2:$Port/swagger"
}
else {
    Write-Host "Published, but the site did not answer on port $Port." -ForegroundColor Yellow
    Write-Host "  Last error : $lastError"
    Write-Host "  Likely causes:"
    Write-Host "   - the IIS website has not been created yet (that is the next step)"
    Write-Host "   - appsettings.Production.json still contains the placeholder values"
    Write-Host "   - the app pool is not set to 'No Managed Code'"
}
