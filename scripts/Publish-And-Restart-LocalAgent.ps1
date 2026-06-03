#Requires -Version 5.1
<#
.SYNOPSIS
  Full pipeline: dotnet publish (optional) + deploy to Program Files + restart.

.PARAMETER ClientConfigPath
  Client JSON under deploy/clients/ (default: amktech).

.PARAMETER RestartOnly
  Skip publish; only restart the running agent.

.PARAMETER DeployOnly
  Skip publish; copy existing dist folder and restart (after manual dotnet publish).
#>
[CmdletBinding()]
param(
    [string] $ClientConfigPath = "deploy\clients\amktech.json",
    [switch] $RestartOnly,
    [switch] $DeployOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot
try {
    if ($RestartOnly) {
        & (Join-Path $PSScriptRoot "Restart-LocalAgent.ps1")
        return
    }

    $clientConfigResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ClientConfigPath)
    $client = Get-Content -Raw $clientConfigResolved | ConvertFrom-Json
    $clientId = $client.clientId
    if ([string]::IsNullOrWhiteSpace($clientId)) {
        throw "clientId missing in $clientConfigResolved"
    }

    $publishDir = Join-Path $repoRoot "dist\localagent-publish\$clientId"

    if (-not $DeployOnly) {
        & (Join-Path $PSScriptRoot "Prepare-ClientPackage.ps1") -ClientConfigPath $clientConfigResolved
    }

    & (Join-Path $PSScriptRoot "Deploy-LocalAgent.ps1") -PublishDir $publishDir
}
finally {
    Pop-Location
}
