namespace WifiAutoSwitcher.Domain;

internal static class NetworkScoring
{
    public static int Calculate(WifiNetwork network)
    {
        var score = network.MaxSignalPercent;
        var radio = network.BestRadioType.ToLowerInvariant();

        if (radio.Contains("ax") || radio.Contains("ac") || radio.Equals("802.11a", StringComparison.Ordinal))
        {
            score += 25;
        }
        else if (radio.Contains("n"))
        {
            score += 10;
        }

        return score;
    }
}
