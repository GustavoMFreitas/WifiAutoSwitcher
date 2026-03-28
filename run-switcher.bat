@echo off
setlocal

cd /d "%~dp0"

set "PUBLISHED_EXE=%~dp0publish\WifiAutoSwitcher\WifiAutoSwitcher.exe"

if exist "%PUBLISHED_EXE%" (
  echo Running WifiAutoSwitcher - published exe...
  "%PUBLISHED_EXE%" --min-improvement 12
  set EXIT_CODE=%ERRORLEVEL%
  goto :END
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Published exe not found and dotnet was not found in PATH.
  echo Run: dotnet publish .\WifiAutoSwitcher\WifiAutoSwitcher.csproj -c Release -o .\publish\WifiAutoSwitcher
  exit /b 1
)

echo Running WifiAutoSwitcher - dotnet fallback...
dotnet run --project "%~dp0WifiAutoSwitcher\WifiAutoSwitcher.csproj" -- --min-improvement 12
set EXIT_CODE=%ERRORLEVEL%

:END
if not "%EXIT_CODE%"=="0" (
  echo [WARN] WifiAutoSwitcher exited with code %EXIT_CODE%.
)

exit /b %EXIT_CODE%
