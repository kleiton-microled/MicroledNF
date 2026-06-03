@echo off
setlocal
REM Full pipeline: client config -> publish -> Inno Setup installer (Windows only)
REM Usage: build-localagent-installer.cmd deploy\clients\microled.example.json

set "CLIENT_CONFIG=%~1"
if "%CLIENT_CONFIG%"=="" (
  echo Usage: build-localagent-installer.cmd deploy\clients\your-client.json
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Prepare-ClientPackage.ps1" -ClientConfigPath "%CLIENT_CONFIG%"
if errorlevel 1 exit /b 1

for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "(Get-Content -Raw '%CLIENT_CONFIG%' | ConvertFrom-Json).clientId"`) do set CLIENT_ID=%%i

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-LocalAgent-Installer.ps1" -PublishDir "%~dp0..\dist\localagent-publish\%CLIENT_ID%" -ClientId "%CLIENT_ID%"
exit /b %errorlevel%
