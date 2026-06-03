#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes LocalAgent for win-x64 and injects per-client appsettings.Client.json.

.PARAMETER ClientConfigPath
  Path to deploy/clients/{clientId}.json

.PARAMETER PublishDir
  Output directory for dotnet publish (default: dist/localagent-publish)

.PARAMETER SkipPublish
  Only inject config into an existing publish folder.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ClientConfigPath,

    [string] $PublishDir = "",

    [switch] $SkipPublish
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "Microled.Nfe.LocalAgent.Api\Microled.Nfe.LocalAgent.Api.csproj"
$templatePath = Join-Path $repoRoot "Microled.Nfe.LocalAgent.Api\appsettings.Client.template.json"

if (-not (Test-Path $ClientConfigPath)) {
    throw "Client config not found: $ClientConfigPath"
}

if (-not (Test-Path $templatePath)) {
    throw "Template not found: $templatePath"
}

$client = Get-Content -Raw -Path $ClientConfigPath | ConvertFrom-Json
$clientId = $client.clientId
if ([string]::IsNullOrWhiteSpace($clientId)) {
    throw "clientId is required in $ClientConfigPath"
}

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $repoRoot "dist\localagent-publish\$clientId"
}

$publishDirResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PublishDir)
New-Item -ItemType Directory -Force -Path $publishDirResolved | Out-Null

if (-not $SkipPublish) {
    Write-Host "Publishing LocalAgent win-x64 (self-contained) to $publishDirResolved ..."
    dotnet publish $projectPath `
        -c Release `
        -p:PublishProfile=LocalAgent-win-x64 `
        -o $publishDirResolved
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    if (-not (Test-Path (Join-Path $publishDirResolved "hostfxr.dll"))) {
        throw "Publish output is not self-contained (hostfxr.dll missing). Check Properties/PublishProfiles/LocalAgent-win-x64.pubxml."
    }
}

$origins = @($client.allowedOrigins | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($origins.Count -eq 0) {
    $origins = @('https://app.amktechsistemas.com.br')
}
# ConvertTo-Json on a single string returns a JSON string, not an array — force array form.
$allowedOriginsJson = ConvertTo-Json -InputObject $origins -Compress

$thumbprint = if ($null -ne $client.certificateThumbprint) { [string]$client.certificateThumbprint } else { "" }
$useProduction = if ($null -ne $client.useProduction) { [string]$client.useProduction.ToString().ToLower() } else { "true" }
$port = if ($null -ne $client.localAgentPort) { [string]$client.localAgentPort } else { "5278" }
$environment = if ($client.environment) { [string]$client.environment } else { "Production" }

$template = Get-Content -Raw -Path $templatePath
$clientSettings = $template `
    -replace '\{\{LOCAL_AGENT_PORT\}\}', $port `
    -replace '\{\{ALLOWED_ORIGINS_JSON\}\}', $allowedOriginsJson `
    -replace '\{\{CERT_THUMBPRINT\}\}', $thumbprint `
    -replace '\{\{MAIN_API_URL\}\}', [string]$client.mainApiUrl `
    -replace '\{\{USE_PRODUCTION\}\}', $useProduction `
    -replace '\{\{ENVIRONMENT\}\}', $environment `
    -replace '\{\{CNPJ\}\}', [string]$client.cnpj `
    -replace '\{\{IM\}\}', [string]$client.inscricaoMunicipal

$clientSettingsPath = Join-Path $publishDirResolved "appsettings.Client.json"
Set-Content -Path $clientSettingsPath -Value $clientSettings -Encoding UTF8
Write-Host "Wrote $clientSettingsPath"

$metadata = @{
    clientId = $clientId
    displayName = $client.displayName
    builtAt = (Get-Date).ToUniversalTime().ToString("o")
    mainApiUrl = $client.mainApiUrl
} | ConvertTo-Json
Set-Content -Path (Join-Path $publishDirResolved "client-package.json") -Value $metadata -Encoding UTF8

Write-Host "Client package ready: $publishDirResolved"
Write-Host "Next: run scripts\Build-LocalAgent-Installer.ps1 -PublishDir `"$publishDirResolved`" -ClientId $clientId"
