@echo off
setlocal

set "TASK_NAME=WifiAutoSwitcher"
set "INTERVAL=%~1"
if "%INTERVAL%"=="" set "INTERVAL=5"

for /f "delims=0123456789" %%i in ("%INTERVAL%") do set "INTERVAL="
if "%INTERVAL%"=="" (
  echo [ERROR] Invalid interval. Use a number of minutes. Example: install-task.bat 3
  exit /b 1
)

if %INTERVAL% LSS 1 (
  echo [ERROR] Interval must be 1 minute or more.
  exit /b 1
)

set "TASK_CMD=\"%~dp0run-switcher.bat\""

echo Creating/updating scheduled task "%TASK_NAME%" to run every %INTERVAL% minute(s)...
schtasks /Create /TN "%TASK_NAME%" /TR "%TASK_CMD%" /SC MINUTE /MO %INTERVAL% /F >nul
if errorlevel 1 (
  echo [ERROR] Could not create scheduled task.
  echo Try running this script as Administrator.
  exit /b 1
)

echo [OK] Task installed.
echo To remove it later, run uninstall-task.bat
exit /b 0
