using System.Diagnostics;
using System.Text;
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

        var escapedTitle = EscapeForPowerShellSingleQuotedString(title);
        var escapedMessage = EscapeForPowerShellSingleQuotedString(message);
        var script = $"""
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml("<toast><visual><binding template='ToastGeneric'><text>{escapedTitle}</text><text>{escapedMessage}</text></binding></visual></toast>")
$toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('WifiAutoSwitcher').Show($toast)
""";

        try
        {
            var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {encodedScript}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(2000);
        }
        catch
        {
            // Notification is best-effort and must never break switching behavior.
        }
    }

    private static string EscapeForPowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
