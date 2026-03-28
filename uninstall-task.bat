@echo off
setlocal

set "TASK_NAME=WifiAutoSwitcher"

echo Removing scheduled task "%TASK_NAME%"...
schtasks /Delete /TN "%TASK_NAME%" /F >nul
if errorlevel 1 (
  echo [INFO] Task was not found or could not be removed.
  exit /b 1
)

echo [OK] Task removed.
exit /b 0
