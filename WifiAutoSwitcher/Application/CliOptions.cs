namespace WifiAutoSwitcher.Application;

internal sealed class CliOptions
{
    public bool DryRun { get; set; }
    public int MinImprovement { get; set; } = 10;
    public int ConnectTimeoutSeconds { get; set; } = 20;
    public int PingAttempts { get; set; } = 3;
    public int PingTimeoutMs { get; set; } = 1000;
    public List<string> PingHosts { get; } = ["1.1.1.1", "8.8.8.8"];

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--dry-run":
                    options.DryRun = true;
                    break;
                case "--min-improvement":
                    options.MinImprovement = int.Parse(ReadValue(args, ref i, arg));
                    break;
                case "--connect-timeout":
                    options.ConnectTimeoutSeconds = int.Parse(ReadValue(args, ref i, arg));
                    break;
                case "--ping-attempts":
                    options.PingAttempts = int.Parse(ReadValue(args, ref i, arg));
                    break;
                case "--ping-timeout":
                    options.PingTimeoutMs = int.Parse(ReadValue(args, ref i, arg));
                    break;
                case "--ping-host":
                    options.PingHosts.Add(ReadValue(args, ref i, arg));
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        return options;

        static string ReadValue(string[] all, ref int index, string flag)
        {
            if (index + 1 >= all.Length)
            {
                throw new ArgumentException($"Missing value for {flag}");
            }

            index++;
            return all[index];
        }
    }
}
