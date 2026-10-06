#Requires -Version 5.1
<#
.SYNOPSIS
  Compiles the on-prem Inno Setup installer from dist/onprem-package.
#>
[CmdletBinding()]
param(
    [string] $PackageDir = "",
    [string] $OutputDir = "",
    [string] $InnoSetupCompiler = "",
    [switch] $SkipPackageBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$issPath = Join-Path $repoRoot "installer\OnPrem.iss"

if (-not $SkipPackageBuild) {
    & (Join-Path $PSScriptRoot "Build-OnPrem-Package.ps1")
}

if ([string]::IsNullOrWhiteSpace($PackageDir)) {
    $PackageDir = Join-Path $repoRoot "dist\onprem-package"
}
$packageResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PackageDir)
if (-not (Test-Path (Join-Path $packageResolved "Microled.Nfe.DesktopLauncher.exe"))) {
    throw "Pacote incompleto (falta Microled.Nfe.DesktopLauncher.exe): $packageResolved"
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
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe"
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

$setupBaseName = "Microled-NFe-AmbienteLocal-1.0.0"
Write-Host "Compiling on-prem installer with ISCC: $iscc"

& $iscc $issPath `
    "/DPackageDir=$packageResolved" `
    "/DMyAppVersion=1.0.0" `
    "/DOutputDir=$outputDirResolved" `
    "/DSetupBaseName=$setupBaseName"

if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE"
}

Write-Host "Installer: $(Join-Path $outputDirResolved "$setupBaseName.exe")"
