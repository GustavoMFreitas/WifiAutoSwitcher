using System.Net.NetworkInformation;
using WifiAutoSwitcher.Application;
using WifiAutoSwitcher.Domain;

namespace WifiAutoSwitcher.Infrastructure;

internal sealed class ConnectivityProbe : IConnectivityProbe
{
    public ConnectivityResult Check(IEnumerable<string> hosts, int attempts, int timeoutMs)
    {
        var latencies = new List<long>();

        using var ping = new Ping();
        foreach (var host in hosts)
        {
            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    var reply = ping.Send(host, timeoutMs);
                    if (reply?.Status == IPStatus.Success)
                    {
                        latencies.Add(reply.RoundtripTime);
                    }
                }
                catch
                {
                    // Ignore and continue probing.
                }
            }
        }

        return latencies.Count == 0
            ? new ConnectivityResult(false, 0)
            : new ConnectivityResult(true, Math.Round(latencies.Average(), 1));
    }
}
