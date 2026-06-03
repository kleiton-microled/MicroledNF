@echo off
setlocal
set "APP_DIR=%~dp0"
cd /d "%APP_DIR%"
if not exist "%APP_DIR%Microled.Nfe.LocalAgent.Api.exe" (
  echo Microled.Nfe.LocalAgent.Api.exe not found in %APP_DIR%
  exit /b 1
)
echo Starting Microled NFe Local Agent (console mode)...
"%APP_DIR%Microled.Nfe.LocalAgent.Api.exe"
