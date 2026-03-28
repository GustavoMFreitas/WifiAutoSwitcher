using WifiAutoSwitcher.Domain;

namespace WifiAutoSwitcher.Application;

internal interface IWifiClient
{
    HashSet<string> GetKnownProfiles();
    List<WifiNetwork> ScanVisibleNetworks();
    CurrentConnection? GetCurrentConnection();
    bool Connect(string profile);
    bool WaitUntilConnectedTo(string targetSsid, TimeSpan timeout);
}

internal interface IConnectivityProbe
{
    ConnectivityResult Check(IEnumerable<string> hosts, int attempts, int timeoutMs);
}

internal interface INetworkSwitchNotifier
{
    void NotifySwitch(string? fromSsid, string toSsid, double averageLatencyMs);
}
