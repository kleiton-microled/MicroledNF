#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes self-contained API, LocalAgent, WPF launcher, Angular (on-prem URLs) and portable PostgreSQL.
#>
[CmdletBinding()]
param(
    [switch] $SkipFrontend,
    [switch] $SkipPostgres
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
if ($env:NODE_OPTIONS) {
    $env:NODE_OPTIONS = ($env:NODE_OPTIONS -replace "--use-system-ca", "").Trim()
}
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$workspaceRoot = Resolve-Path (Join-Path $repoRoot "..")
$frontRoot = Join-Path $workspaceRoot "MicroledNfFrontEnd"
$output = Join-Path $repoRoot "dist\onprem-package"
$script:NpmCmd = $null
$script:NpxCmd = $null

function Publish-Dotnet([string] $project, [string] $dest, [string] $tfm) {
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    & dotnet publish $project -c Release -r win-x64 --self-contained true -f $tfm -o $dest
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish falhou: $project"
    }
}

function Find-NpmOnDisk {
    $candidates = @(
        (Join-Path $env:ProgramFiles "nodejs\npm.cmd"),
        (Join-Path ${env:ProgramFiles(x86)} "nodejs\npm.cmd"),
        (Join-Path $env:LOCALAPPDATA "Programs\nodejs\npm.cmd"),
        (Join-Path $env:APPDATA "nvm"),
        (Join-Path $env:LOCALAPPDATA "fnm_multishells")
    )
    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path -PathType Leaf)) {
            return (Resolve-Path $path).Path
        }
    }

    $whereNpm = Get-Command npm.cmd -ErrorAction SilentlyContinue
    if ($whereNpm) {
        return $whereNpm.Source
    }

    return $null
}

function Install-PortableNode {
    $version = "v22.14.0"
    $nodeHome = Join-Path $repoRoot "dist\tools\node"
    $npmCmd = Join-Path $nodeHome "npm.cmd"
    if (Test-Path $npmCmd) {
        Write-Host "Usando Node portátil em $nodeHome"
        return $npmCmd
    }

    $zipName = "node-$version-win-x64.zip"
    $url = "https://nodejs.org/dist/$version/$zipName"
    $zipPath = Join-Path $env:TEMP $zipName
    Write-Host "Node/npm não estão no PATH. Baixando Node $version (somente para o build, não vai no instalador do cliente)..."
    Write-Host $url
    Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing

    $extractRoot = Join-Path $env:TEMP "microled-node-extract"
    if (Test-Path $extractRoot) {
        Remove-Item -Recurse -Force $extractRoot
    }
    New-Item -ItemType Directory -Force -Path $extractRoot | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force

    $extracted = Get-ChildItem -Path $extractRoot -Directory | Where-Object {
        Test-Path (Join-Path $_.FullName "npm.cmd")
    } | Select-Object -First 1
    if (-not $extracted) {
        throw "Zip do Node.js não contém npm.cmd."
    }

    New-Item -ItemType Directory -Force -Path $nodeHome | Out-Null
    Copy-Item -Path (Join-Path $extracted.FullName "*") -Destination $nodeHome -Recurse -Force
    if (-not (Test-Path $npmCmd)) {
        throw "Falha ao preparar Node portátil em $nodeHome"
    }

    return $npmCmd
}

function Ensure-Npm {
    $found = Find-NpmOnDisk
    if (-not $found) {
        $found = Install-PortableNode
    }

    $script:NpmCmd = $found
    $nodeDir = Split-Path $found -Parent
    $script:NodeExe = Join-Path $nodeDir "node.exe"
    $script:NpmCli = Join-Path $nodeDir "node_modules\npm\bin\npm-cli.js"
    $script:NpxCli = Join-Path $nodeDir "node_modules\npm\bin\npx-cli.js"
    if ($env:Path -notlike "*$nodeDir*") {
        $env:Path = "$nodeDir;$env:Path"
    }
    # NODE_OPTIONS não aceita --use-system-ca. Proxy/antivirus quebra o CA bundle do Node portátil.
    $env:NODE_TLS_REJECT_UNAUTHORIZED = "0"
    $env:npm_config_strict_ssl = "false"
    Write-Host "npm: $script:NpmCmd"
}

function Invoke-Npm {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]] $NpmArgs)
    if (Test-Path $script:NpmCli) {
        & $script:NodeExe $script:NpmCli @NpmArgs
    }
    else {
        & $script:NpmCmd @NpmArgs
    }
}

function Invoke-Npx {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]] $NpxArgs)
    if (Test-Path $script:NpxCli) {
        & $script:NodeExe $script:NpxCli @NpxArgs
    }
    else {
        & (Join-Path (Split-Path $script:NpmCmd -Parent) "npx.cmd") @NpxArgs
    }
}

if (Test-Path $output) {
    Remove-Item -Recurse -Force $output
}
New-Item -ItemType Directory -Force -Path $output | Out-Null

Write-Host "Publicando API..."
Publish-Dotnet (Join-Path $repoRoot "Microled.Nfe.Service.Api\Microled.Nfe.Service.Api.csproj") (Join-Path $output "api") "net8.0"

Write-Host "Publicando LocalAgent..."
Publish-Dotnet (Join-Path $repoRoot "Microled.Nfe.LocalAgent.Api\Microled.Nfe.LocalAgent.Api.csproj") (Join-Path $output "agent") "net8.0"

Write-Host "Publicando launcher..."
Publish-Dotnet (Join-Path $repoRoot "Microled.Nfe.DesktopLauncher\Microled.Nfe.DesktopLauncher.csproj") $output "net8.0-windows"

if (-not $SkipFrontend) {
    if (-not (Test-Path (Join-Path $frontRoot "package.json"))) {
        throw "Frontend não encontrado em $frontRoot"
    }
    Ensure-Npm
    Write-Host "Compilando Angular..."
    Push-Location $frontRoot
    try {
        $ngCmd = Join-Path $frontRoot "node_modules\@angular\cli\bin\ng.js"
        if (-not (Test-Path $ngCmd)) {
            Invoke-Npm install --no-audit --no-fund
            if ($LASTEXITCODE -ne 0) { throw "npm install falhou" }
        }
        if (-not (Test-Path $ngCmd)) {
            throw "Angular CLI não encontrado após npm install: $ngCmd"
        }
        & $script:NodeExe $ngCmd build --configuration production
        if ($LASTEXITCODE -ne 0) { throw "ng build falhou" }
    }
    finally {
        Pop-Location
    }

    $browserDist = Join-Path $frontRoot "dist\microled-nf-front-end\browser"
    if (-not (Test-Path $browserDist)) {
        $browserDist = Join-Path $frontRoot "dist\microled-nf-front-end"
    }
    if (-not (Test-Path (Join-Path $browserDist "index.html"))) {
        throw "Build Angular não gerou index.html em $browserDist"
    }

    $wwwroot = Join-Path $output "api\wwwroot"
    New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
    Copy-Item -Path (Join-Path $browserDist "*") -Destination $wwwroot -Recurse -Force
    Copy-Item -Path (Join-Path $frontRoot "public\runtime-config.onprem.json") -Destination (Join-Path $wwwroot "runtime-config.json") -Force
}

if (-not $SkipPostgres) {
    Write-Host "Obtendo PostgreSQL portátil..."
    & (Join-Path $PSScriptRoot "Download-PortablePostgres.ps1") -Destination (Join-Path $output "pgsql")
}

Write-Host "Pacote on-prem gerado em $output"
Write-Host "Execute Microled.Nfe.DesktopLauncher.exe nessa pasta (após o instalador, o atalho aponta para cá)."
