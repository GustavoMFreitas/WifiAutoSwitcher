using WifiAutoSwitcher.Domain;

namespace WifiAutoSwitcher.Application;

internal sealed class WifiSwitcherApp(
    IWifiClient wifiClient,
    IConnectivityProbe connectivityProbe,
    INetworkSwitchNotifier networkSwitchNotifier)
{
    public int Run(CliOptions options)
    {
        var knownProfiles = wifiClient.GetKnownProfiles();
        if (knownProfiles.Count == 0)
        {
            Console.WriteLine("No saved Wi-Fi profiles were found.");
            return 0;
        }

        var visibleNetworks = wifiClient.ScanVisibleNetworks();
        var knownVisible = visibleNetworks
            .Where(n => knownProfiles.Contains(n.Ssid, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (knownVisible.Count == 0)
        {
            Console.WriteLine("No known saved networks are currently visible.");
            return 0;
        }

        var current = wifiClient.GetCurrentConnection();
        var ranked = knownVisible
            .Select(n => new RankedNetwork(n, NetworkScoring.Calculate(n)))
            .OrderByDescending(n => n.Score)
            .ToList();

        Console.WriteLine("Known visible networks (best first):");
        foreach (var n in ranked)
        {
            var currentMark = string.Equals(current?.Ssid, n.Network.Ssid, StringComparison.OrdinalIgnoreCase) ? " (current)" : string.Empty;
            Console.WriteLine($"- {n.Network.Ssid,-30} signal={n.Network.MaxSignalPercent,3}% radio={n.Network.BestRadioType,-12} score={n.Score,3}{currentMark}");
        }

        var best = ranked[0];
        if (current is not null && string.Equals(current.Ssid, best.Network.Ssid, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Already connected to the best known network: {best.Network.Ssid}");
            return 0;
        }

        if (current is not null)
        {
            var currentScore = ranked
                .Where(n => string.Equals(n.Network.Ssid, current.Ssid, StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Score)
                .DefaultIfEmpty(0)
                .Max();

            if (best.Score - currentScore < options.MinImprovement)
            {
                Console.WriteLine($"Best network improvement is below threshold ({options.MinImprovement}). Staying on {current.Ssid}.");
                return 0;
            }
        }

        Console.WriteLine($"Switch target: {best.Network.Ssid}");
        if (options.DryRun)
        {
            Console.WriteLine("Dry run enabled; no connection change executed.");
            return 0;
        }

        var previousSsid = current?.Ssid;
        if (!wifiClient.Connect(best.Network.Ssid))
        {
            Console.Error.WriteLine($"Failed to start connection to {best.Network.Ssid}");
            return 1;
        }

        if (!wifiClient.WaitUntilConnectedTo(best.Network.Ssid, TimeSpan.FromSeconds(options.ConnectTimeoutSeconds)))
        {
            Console.Error.WriteLine($"Could not confirm connection to {best.Network.Ssid} within timeout.");
            TryRollback(previousSsid);
            return 1;
        }

        var connectivity = connectivityProbe.Check(options.PingHosts, options.PingAttempts, options.PingTimeoutMs);
        if (!connectivity.Success)
        {
            Console.Error.WriteLine("Connected to target network but internet reachability test failed.");
            TryRollback(previousSsid);
            return 1;
        }

        Console.WriteLine($"Connected to {best.Network.Ssid}. Internet reachable. Avg latency: {connectivity.AverageLatencyMs} ms");
        if (!string.Equals(previousSsid, best.Network.Ssid, StringComparison.OrdinalIgnoreCase))
        {
            networkSwitchNotifier.NotifySwitch(previousSsid, best.Network.Ssid, connectivity.AverageLatencyMs);
        }

        return 0;
    }

    private void TryRollback(string? previousSsid)
    {
        if (string.IsNullOrWhiteSpace(previousSsid))
        {
            return;
        }

        Console.WriteLine($"Rolling back to previous network: {previousSsid}");
        wifiClient.Connect(previousSsid);
    }
}
