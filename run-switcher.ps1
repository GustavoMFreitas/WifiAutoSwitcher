$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishedExe = Join-Path $scriptDir "publish\WifiAutoSwitcher\WifiAutoSwitcher.exe"

if (Test-Path $publishedExe) {
    & $publishedExe --min-improvement 12
    exit $LASTEXITCODE
}

$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    exit 1
}

$projectPath = Join-Path $scriptDir "WifiAutoSwitcher\WifiAutoSwitcher.csproj"
& $dotnetCmd.Source run --project $projectPath -- --min-improvement 12
exit $LASTEXITCODE
