#Requires -Version 5.1
<#
.SYNOPSIS
  Downloads portable PostgreSQL 16 Windows x64 binaries (no Docker).
#>
[CmdletBinding()]
param(
    [string] $Destination = "",
    [string] $Url = "https://get.enterprisedb.com/postgresql/postgresql-16.9-1-windows-x64-binaries.zip"
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $repoRoot "dist\pgsql"
}

$destResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
$marker = Join-Path $destResolved "bin\pg_ctl.exe"
if (Test-Path $marker) {
    Write-Host "PostgreSQL portátil já existe em $destResolved"
    return
}

New-Item -ItemType Directory -Force -Path $destResolved | Out-Null
$zipPath = Join-Path $env:TEMP "microled-pgsql-win-x64.zip"
Write-Host "Baixando PostgreSQL portátil..."
Write-Host $Url
Invoke-WebRequest -Uri $Url -OutFile $zipPath

$extractRoot = Join-Path $env:TEMP "microled-pgsql-extract"
if (Test-Path $extractRoot) {
    Remove-Item -Recurse -Force $extractRoot
}
New-Item -ItemType Directory -Force -Path $extractRoot | Out-Null
Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force

$pgsqlDir = Get-ChildItem -Path $extractRoot -Directory | Where-Object {
    Test-Path (Join-Path $_.FullName "bin\pg_ctl.exe")
} | Select-Object -First 1

if (-not $pgsqlDir) {
    $nested = Get-ChildItem -Path $extractRoot -Recurse -Filter "pg_ctl.exe" | Select-Object -First 1
    if ($nested) {
        $pgsqlDir = Get-Item $nested.Directory.Parent.FullName
    }
}

if (-not $pgsqlDir) {
    throw "Zip do PostgreSQL não contém bin\pg_ctl.exe. Extraia manualmente para $destResolved"
}

# EDB zip includes pgAdmin (~1GB). The on-prem launcher only needs the server binaries.
$keep = @("bin", "lib", "share", "include")
foreach ($name in $keep) {
    $source = Join-Path $pgsqlDir.FullName $name
    if (Test-Path $source) {
        Copy-Item -Path $source -Destination (Join-Path $destResolved $name) -Recurse -Force
    }
}

if (-not (Test-Path $marker)) {
    throw "Falha ao copiar bin\pg_ctl.exe para $destResolved"
}

Write-Host "PostgreSQL extraído para $destResolved (sem pgAdmin)"
