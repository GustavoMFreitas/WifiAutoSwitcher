using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WifiAutoSwitcher.Application;
using WifiAutoSwitcher.Domain;

namespace WifiAutoSwitcher.Infrastructure;

internal sealed class WifiNetsh : IWifiClient
{
    private static readonly Regex SsidLine = new(@"^SSID\s+\d+\s*:\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex SignalLine = new(@"^(Signal|Sinal)\s*:\s*(\d+)%", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RadioLine = new(@"^(Radio type|Tipo de r[aá]dio)\s*:\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public HashSet<string> GetKnownProfiles()
    {
        var output = RunNetsh("wlan show profiles");
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            var separator = line.IndexOf(':');
            if (separator < 0)
            {
                continue;
            }

            var key = line[..separator].Trim().ToLowerInvariant();
            var value = line[(separator + 1)..].Trim();

            if ((key.Contains("profile") || key.Contains("perfis")) && !string.IsNullOrWhiteSpace(value))
            {
                set.Add(value);
            }
        }

        return set;
    }

    public List<WifiNetwork> ScanVisibleNetworks()
    {
        TriggerRefreshScan();
        var output = RunNetsh("wlan show networks mode=bssid");
        var networks = new Dictionary<string, WifiNetwork>(StringComparer.OrdinalIgnoreCase);
        WifiNetwork? current = null;

        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var ssidMatch = SsidLine.Match(line);
            if (ssidMatch.Success)
            {
                var ssid = ssidMatch.Groups[1].Value.Trim();
                if (ssid.Length == 0)
                {
                    current = null;
                    continue;
                }

                if (!networks.TryGetValue(ssid, out current))
                {
                    current = new WifiNetwork(ssid);
                    networks.Add(ssid, current);
                }

                continue;
            }

            if (current is null)
            {
                continue;
            }

            var signalMatch = SignalLine.Match(line);
            if (signalMatch.Success && int.TryParse(signalMatch.Groups[2].Value, out var signal))
            {
                current.MaxSignalPercent = Math.Max(current.MaxSignalPercent, signal);
                continue;
            }

            var radioMatch = RadioLine.Match(line);
            if (radioMatch.Success)
            {
                var radio = radioMatch.Groups[2].Value.Trim();
                if (RadioPriority(radio) > RadioPriority(current.BestRadioType))
                {
                    current.BestRadioType = radio;
                }
            }
        }

        return networks.Values.ToList();
    }

    public CurrentConnection? GetCurrentConnection()
    {
        var output = RunNetsh("wlan show interfaces");
        string? ssid = null;
        var connected = false;

        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            var separator = line.IndexOf(':');
            if (separator < 0)
            {
                continue;
            }

            var key = line[..separator].Trim().ToLowerInvariant();
            var value = line[(separator + 1)..].Trim();

            if (key.Equals("ssid", StringComparison.OrdinalIgnoreCase))
            {
                ssid = value;
                continue;
            }

            if (key.Contains("state") || key.Contains("estado"))
            {
                var state = value.ToLowerInvariant();
                connected = state.Contains("connected") || state.Contains("conectado");
            }
        }

        return connected && !string.IsNullOrWhiteSpace(ssid)
            ? new CurrentConnection(ssid)
            : null;
    }

    public bool Connect(string profile)
    {
        var output = RunNetsh($"wlan connect name=\"{EscapeForNetsh(profile)}\"", throwOnError: false);
        var lowered = output.ToLowerInvariant();
        return lowered.Contains("success") || lowered.Contains("sucesso") || lowered.Contains("completed") || lowered.Contains("conclu");
    }

    public bool WaitUntilConnectedTo(string targetSsid, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < until)
        {
            var current = GetCurrentConnection();
            if (current is not null && string.Equals(current.Ssid, targetSsid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Thread.Sleep(1200);
        }

        return false;
    }

    private static void TriggerRefreshScan()
    {
        // netsh often reports cached scan results unless the WLAN stack is actively scanned first.
        if (WlanApi.TryTriggerScan())
        {
            Thread.Sleep(2200);
            return;
        }

        Thread.Sleep(1200);
    }

    private static string RunNetsh(string arguments, bool throwOnError = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Could not start netsh process.");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var combined = string.IsNullOrWhiteSpace(error) ? output : output + Environment.NewLine + error;

        if (throwOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"netsh failed ({arguments}): {combined}");
        }

        return combined;
    }

    private static int RadioPriority(string radio)
    {
        var value = radio.ToLowerInvariant();
        if (value.Contains("ax")) return 6;
        if (value.Contains("ac")) return 5;
        if (value.Contains("a")) return 4;
        if (value.Contains("n")) return 3;
        if (value.Contains("g")) return 2;
        if (value.Contains("b")) return 1;
        return 0;
    }

    private static string EscapeForNetsh(string value) => value.Replace("\"", "\\\"");
}
