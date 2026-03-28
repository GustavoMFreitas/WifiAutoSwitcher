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

set "SCRIPT_DIR=%~dp0"
set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"
set "PS_SCRIPT=%SCRIPT_DIR%\run-switcher.ps1"
set "VBS_SCRIPT=%SCRIPT_DIR%\run-switcher.vbs"

if not exist "%PS_SCRIPT%" (
  echo [ERROR] Missing PowerShell runner: "%PS_SCRIPT%"
  exit /b 1
)

if not exist "%VBS_SCRIPT%" (
  echo [ERROR] Missing VBScript runner: "%VBS_SCRIPT%"
  exit /b 1
)

for /f "usebackq delims=" %%u in (`whoami`) do set "TASK_USER=%%u"

set "TASK_XML=%TEMP%\%TASK_NAME%.xml"

(
  echo ^<?xml version="1.0"?^>
  echo ^<Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task"^>
  echo   ^<RegistrationInfo^>
  echo     ^<Description^>Automatically evaluates known Wi-Fi networks and switches when better.^</Description^>
  echo   ^</RegistrationInfo^>
  echo   ^<Triggers^>
  echo     ^<TimeTrigger^>
  echo       ^<Repetition^>
  echo         ^<Interval^>PT%INTERVAL%M^</Interval^>
  echo         ^<StopAtDurationEnd^>false^</StopAtDurationEnd^>
  echo       ^</Repetition^>
  echo       ^<StartBoundary^>2026-01-01T00:00:00^</StartBoundary^>
  echo       ^<Enabled^>true^</Enabled^>
  echo     ^</TimeTrigger^>
  echo   ^</Triggers^>
  echo   ^<Principals^>
  echo     ^<Principal id="Author"^>
  echo       ^<UserId^>%TASK_USER%^</UserId^>
  echo       ^<LogonType^>InteractiveToken^</LogonType^>
  echo       ^<RunLevel^>LeastPrivilege^</RunLevel^>
  echo     ^</Principal^>
  echo   ^</Principals^>
  echo   ^<Settings^>
  echo     ^<MultipleInstancesPolicy^>IgnoreNew^</MultipleInstancesPolicy^>
  echo     ^<DisallowStartIfOnBatteries^>false^</DisallowStartIfOnBatteries^>
  echo     ^<StopIfGoingOnBatteries^>false^</StopIfGoingOnBatteries^>
  echo     ^<AllowHardTerminate^>true^</AllowHardTerminate^>
  echo     ^<StartWhenAvailable^>true^</StartWhenAvailable^>
  echo     ^<RunOnlyIfNetworkAvailable^>false^</RunOnlyIfNetworkAvailable^>
  echo     ^<IdleSettings^>
  echo       ^<StopOnIdleEnd^>false^</StopOnIdleEnd^>
  echo       ^<RestartOnIdle^>false^</RestartOnIdle^>
  echo     ^</IdleSettings^>
  echo     ^<AllowStartOnDemand^>true^</AllowStartOnDemand^>
  echo     ^<Enabled^>true^</Enabled^>
  echo     ^<Hidden^>true^</Hidden^>
  echo     ^<RunOnlyIfIdle^>false^</RunOnlyIfIdle^>
  echo     ^<WakeToRun^>false^</WakeToRun^>
  echo     ^<ExecutionTimeLimit^>PT5M^</ExecutionTimeLimit^>
  echo     ^<Priority^>7^</Priority^>
  echo   ^</Settings^>
  echo   ^<Actions Context="Author"^>
  echo     ^<Exec^>
  echo       ^<Command^>wscript.exe^</Command^>
  echo       ^<Arguments^>//B //Nologo "%VBS_SCRIPT%"^</Arguments^>
  echo       ^<WorkingDirectory^>%SCRIPT_DIR%^</WorkingDirectory^>
  echo     ^</Exec^>
  echo   ^</Actions^>
  echo ^</Task^>
) > "%TASK_XML%"

echo Creating/updating scheduled task "%TASK_NAME%" to run every %INTERVAL% minute^(s^) in background...
schtasks /Create /TN "%TASK_NAME%" /XML "%TASK_XML%" /F >nul
set "CREATE_EXIT=%ERRORLEVEL%"
del "%TASK_XML%" >nul 2>&1

if not "%CREATE_EXIT%"=="0" (
  echo [ERROR] Could not create scheduled task.
  echo Try running this script as Administrator.
  exit /b 1
)

echo [OK] Task installed.
echo - Runs hidden (no cmd window).
echo - Allowed on battery power.
echo To remove it later, run uninstall-task.bat
exit /b 0
