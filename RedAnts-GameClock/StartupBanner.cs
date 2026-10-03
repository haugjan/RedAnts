using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RedAnts.GameClock;

public static class StartupBanner
{
    public static void Print(ICollection<string> urls)
    {
        var ports = urls.Select(PortOf).Distinct().ToList();
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .ToList();

        Console.WriteLine();
        Console.WriteLine("  Red Ants Matchuhr laeuft:");
        foreach (var port in ports)
        {
            Console.WriteLine($"    http://localhost:{port}/");
            foreach (var address in addresses) Console.WriteLine($"    http://{address}:{port}/");
        }
        Console.WriteLine("  Beenden mit Ctrl+C");
        Console.WriteLine();
    }

    static int PortOf(string url) =>
        new Uri(url.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost").Replace("*", "localhost").Replace("+", "localhost")).Port;
}
