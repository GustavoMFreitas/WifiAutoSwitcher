using System.Diagnostics;
using WifiAutoSwitcher.Application;

namespace WifiAutoSwitcher.Infrastructure;

internal sealed class WindowsToastNetworkSwitchNotifier : INetworkSwitchNotifier
{
    public void NotifySwitch(string? fromSsid, string toSsid, double averageLatencyMs)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var fromText = string.IsNullOrWhiteSpace(fromSsid) ? "unknown network" : fromSsid;
        var title = "Wi-Fi switched";
        var message = $"{fromText} -> {toSsid} | Avg latency: {averageLatencyMs:0.##} ms";

        var escapedTitle = EscapeForPowerShellLiteralString(title);
        var escapedMessage = EscapeForPowerShellLiteralString(message);
        var script = $"""
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$notify = New-Object System.Windows.Forms.NotifyIcon
$notify.Icon = [System.Drawing.SystemIcons]::Information
$notify.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Info
$notify.BalloonTipTitle = '{escapedTitle}'
$notify.BalloonTipText = '{escapedMessage}'
$notify.Visible = $true
$notify.ShowBalloonTip(5000)
Start-Sleep -Milliseconds 5500
$notify.Dispose()
""";

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{EscapeForPowerShellCommandArgument(script)}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(7000);
        }
        catch
        {
            // Notification is best-effort and must never break switching behavior.
        }
    }

    private static string EscapeForPowerShellLiteralString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static string EscapeForPowerShellCommandArgument(string value)
    {
        return value.Replace("\"", "`\"", StringComparison.Ordinal);
    }
}
