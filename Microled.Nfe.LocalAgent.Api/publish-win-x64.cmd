@echo off
setlocal

echo Publishing Microled.Nfe.LocalAgent.Api for win-x64...
dotnet publish "%~dp0Microled.Nfe.LocalAgent.Api.csproj" -p:PublishProfile=LocalAgent-win-x64

if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

echo.
echo Publish completed successfully.
echo Output: %~dp0bin\Release\net8.0\publish\win-x64\
endlocal
