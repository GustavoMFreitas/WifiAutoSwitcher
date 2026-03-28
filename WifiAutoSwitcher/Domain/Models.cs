namespace WifiAutoSwitcher.Domain;

internal sealed class WifiNetwork(string ssid)
{
    public string Ssid { get; } = ssid;
    public int MaxSignalPercent { get; set; }
    public string BestRadioType { get; set; } = "unknown";
}

internal sealed record CurrentConnection(string Ssid);

internal sealed record RankedNetwork(WifiNetwork Network, int Score);

internal sealed record ConnectivityResult(bool Success, double AverageLatencyMs);
