using System.Runtime.InteropServices;
using WifiAutoSwitcher.Application;
using WifiAutoSwitcher.Infrastructure;

return ProgramMain.Run(args);

internal static class ProgramMain
{
    public static int Run(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.Error.WriteLine("This app only supports Windows (uses netsh wlan).");
            return 1;
        }

        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var app = new WifiSwitcherApp(
            new WifiNetsh(),
            new ConnectivityProbe(),
            new WindowsToastNetworkSwitchNotifier());
        return app.Run(options);
    }
}
