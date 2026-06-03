#Requires -Version 5.1
<#
.SYNOPSIS
  Stops Local Agent, copies a publish folder to Program Files, then restarts.

.PARAMETER PublishDir
  Source folder (default: dist\localagent-publish\amktech).

.PARAMETER InstallDir
  Target install folder (default: C:\Program Files\Microled\NfeLocalAgent).
#>
[CmdletBinding()]
param(
    [string] $PublishDir = "",
    [string] $InstallDir = "C:\Program Files\Microled\NfeLocalAgent"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $repoRoot "dist\localagent-publish\amktech"
}

$publishResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PublishDir)
if (-not (Test-Path (Join-Path $publishResolved "Microled.Nfe.LocalAgent.Api.exe"))) {
    throw "Publish folder invalid: $publishResolved"
}

Write-Host "Stopping Microled.Nfe.LocalAgent.Api (if running)..."
$stopped = $false
foreach ($proc in Get-Process -Name "Microled.Nfe.LocalAgent.Api" -ErrorAction SilentlyContinue) {
    Stop-Process -Id $proc.Id -Force
    $stopped = $true
}
if ($stopped) {
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        if (-not (Get-Process -Name "Microled.Nfe.LocalAgent.Api" -ErrorAction SilentlyContinue)) {
            break
        }
        Start-Sleep -Milliseconds 500
    }
    if (Get-Process -Name "Microled.Nfe.LocalAgent.Api" -ErrorAction SilentlyContinue) {
        throw "Could not stop Microled.Nfe.LocalAgent.Api. Close it manually and retry."
    }
    Start-Sleep -Seconds 2
}

Write-Host "Copying from $publishResolved to $InstallDir (elevation required)..."
$robocopyLine = "robocopy `"$publishResolved`" `"$InstallDir`" /E /XO /XF appsettings.Client.json /NFL /NDL /NJH /NJS /nc /ns /np; if (`$LASTEXITCODE -ge 8) { exit 1 } else { exit 0 }"
$elevatedScript = Join-Path $env:TEMP "microled-deploy-robocopy.ps1"
Set-Content -Path $elevatedScript -Value $robocopyLine -Encoding UTF8
$p = Start-Process powershell -Verb RunAs -Wait -PassThru -ArgumentList @(
    "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $elevatedScript
)
if ($p.ExitCode -ne 0) {
    throw "robocopy failed with exit code $($p.ExitCode). Run this script as Administrator."
}

$vbs = Join-Path $InstallDir "StartLocalAgent.vbs"
if (-not (Test-Path $vbs)) {
    throw "StartLocalAgent.vbs not found in $InstallDir"
}

Write-Host "Starting Local Agent..."
Start-Process -FilePath "wscript.exe" -ArgumentList "`"$vbs`""

Start-Sleep -Seconds 4
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5278/api/local/health" -TimeoutSec 10
    Write-Host "Health: $($health | ConvertTo-Json -Compress)"
}
catch {
    Write-Warning "Agent started but health check failed: $($_.Exception.Message)"
}

Write-Host "Deploy completed."
