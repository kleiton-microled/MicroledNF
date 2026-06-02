#Requires -Version 5.1
<#
.SYNOPSIS
  Compiles Inno Setup installer for a prepared LocalAgent publish folder.

.PARAMETER PublishDir
  Folder containing Microled.Nfe.LocalAgent.Api.exe (from Prepare-ClientPackage.ps1).

.PARAMETER ClientId
  Client identifier used in output file name.

.PARAMETER OutputDir
  Directory for the generated setup.exe (default: dist/installers).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDir,

    [Parameter(Mandatory = $true)]
    [string] $ClientId,

    [string] $OutputDir = "",

    [string] $InnoSetupCompiler = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$issPath = Join-Path $repoRoot "installer\LocalAgent.iss"

$publishDirResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PublishDir)
if (-not (Test-Path (Join-Path $publishDirResolved "Microled.Nfe.LocalAgent.Api.exe"))) {
    throw "Publish folder missing Microled.Nfe.LocalAgent.Api.exe: $publishDirResolved"
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "dist\installers"
}
$outputDirResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)
New-Item -ItemType Directory -Force -Path $outputDirResolved | Out-Null

$iscc = $InnoSetupCompiler
if ([string]::IsNullOrWhiteSpace($iscc)) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $iscc = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($iscc) -or -not (Test-Path $iscc)) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6 or pass -InnoSetupCompiler."
}

$version = "1.0.0"
$clientPackage = Join-Path $publishDirResolved "client-package.json"
if (Test-Path $clientPackage) {
    $meta = Get-Content -Raw $clientPackage | ConvertFrom-Json
    if ($meta.clientId) { $ClientId = $meta.clientId }
}

$setupBaseName = "Microled-NFe-LocalAgent-$ClientId-$version"
Write-Host "Compiling installer with ISCC: $iscc"

& $iscc $issPath `
    "/DPublishDir=$publishDirResolved" `
    "/DClientId=$ClientId" `
    "/DMyAppVersion=$version" `
    "/DOutputDir=$outputDirResolved" `
    "/DSetupBaseName=$setupBaseName"

if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE"
}

$setupExe = Join-Path $outputDirResolved "$setupBaseName.exe"
Write-Host "Installer created: $setupExe"
