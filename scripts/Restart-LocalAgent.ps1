#Requires -Version 5.1
<#
.SYNOPSIS
  Stops and starts the installed Local Agent (no file copy).
#>
[CmdletBinding()]
param(
    [string] $InstallDir = "C:\Program Files\Microled\NfeLocalAgent"
)

$ErrorActionPreference = "Stop"

Write-Host "Stopping Microled.Nfe.LocalAgent.Api (if running)..."
foreach ($proc in Get-Process -Name "Microled.Nfe.LocalAgent.Api" -ErrorAction SilentlyContinue) {
    Stop-Process -Id $proc.Id -Force
}

$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline) {
    if (-not (Get-Process -Name "Microled.Nfe.LocalAgent.Api" -ErrorAction SilentlyContinue)) {
        break
    }
    Start-Sleep -Milliseconds 500
}

Start-Sleep -Seconds 1

$vbs = Join-Path $InstallDir "StartLocalAgent.vbs"
if (-not (Test-Path $vbs)) {
    throw "StartLocalAgent.vbs not found: $vbs"
}

Write-Host "Starting Local Agent..."
Start-Process -FilePath "wscript.exe" -ArgumentList "`"$vbs`""

Start-Sleep -Seconds 4
$health = Invoke-RestMethod -Uri "http://localhost:5278/api/local/health" -TimeoutSec 10
Write-Host "Health: $($health | ConvertTo-Json -Compress)"
