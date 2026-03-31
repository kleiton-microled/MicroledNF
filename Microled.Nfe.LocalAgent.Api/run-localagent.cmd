@echo off
setlocal

set "APP_DIR=%~dp0"
set "EXE_PATH=%APP_DIR%Microled.Nfe.LocalAgent.Api.exe"

if not exist "%EXE_PATH%" (
  echo Microled.Nfe.LocalAgent.Api.exe not found in:
  echo %APP_DIR%
  echo.
  echo Publish the project first using publish-win-x64.cmd
  exit /b 1
)

echo Starting Microled.Nfe.LocalAgent.Api in the current user session...
echo Keep this window open while using the LocalAgent.
echo.

"%EXE_PATH%"
endlocal
