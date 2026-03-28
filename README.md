# WifiAutoSwitcher

Windows console tool that switches your notebook to the best **already-known** Wi-Fi network (saved profile) currently in range.

## What it does

- Reads your saved Wi-Fi profiles (`netsh wlan show profiles`)
- Scans visible Wi-Fi networks (`netsh wlan show networks mode=bssid`)
- Keeps only SSIDs that are both visible and already known
- Scores each known network (signal + radio type preference)
- Connects to the best candidate only if it is enough better than current
- Verifies internet reachability with ping after switching
- Rolls back to previous network if connectivity test fails

## Requirements

- Windows 10/11
- .NET 9 SDK (for build)
- Run terminal as user with permission to manage Wi-Fi profiles

## Build

```powershell
dotnet build .\\WifiAutoSwitcher.sln
```

## Publish (recommended for scheduler)

```powershell
dotnet publish .\WifiAutoSwitcher\WifiAutoSwitcher.csproj -c Release -o .\publish\WifiAutoSwitcher
```

## Run

```powershell
dotnet run --project .\WifiAutoSwitcher\WifiAutoSwitcher.csproj
```

### Useful options

```powershell
# Show ranking, do not switch
dotnet run --project .\WifiAutoSwitcher\WifiAutoSwitcher.csproj -- --dry-run

# Require at least 15 score points improvement before switching
dotnet run --project .\WifiAutoSwitcher\WifiAutoSwitcher.csproj -- --min-improvement 15

# Customize connection timeout and ping settings
dotnet run --project .\WifiAutoSwitcher\WifiAutoSwitcher.csproj -- --connect-timeout 25 --ping-attempts 4 --ping-timeout 1200
```

## Auto-run (Task Scheduler)

Use the included batch files.

### Install

From `\WifiAutoSwitcher`:

```powershell
# Default: run every 5 minutes
.\install-task.bat

# Custom interval in minutes (example: 3)
.\install-task.bat 3
```

This creates a scheduled task named `WifiAutoSwitcher` that runs `run-switcher.bat`.
If a published exe exists at `.\publish\WifiAutoSwitcher\WifiAutoSwitcher.exe`, the script uses it automatically.

### Uninstall

```powershell
.\uninstall-task.bat
```

### Manual run

```powershell
.\run-switcher.bat
```

## Notes

- This version assumes your saved profile name matches the SSID (common default behavior in Windows).
- Hidden SSIDs are not considered unless visible in scan output.
