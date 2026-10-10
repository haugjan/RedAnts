using System.Net.Sockets;
using System.Text;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Led;

public sealed class SednaLink(PayloadStore payload, ILogger<SednaLink> log)
{
    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);

    public bool Configured => payload.Hall.Devices.SednaHost.Length > 0;

    public bool Connected { get; private set; }

    public string? Fault { get; private set; }

    public string? LastCommand { get; private set; }

    public async Task SendAsync(string command, CancellationToken stop)
    {
        var devices = payload.Hall.Devices;
        if (devices.SednaHost.Length == 0) throw new InvalidOperationException("Keine LED-Wand (sedna) konfiguriert");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
        timeout.CancelAfter(ConnectTimeout);
        try
        {
            using var client = new TcpClient { NoDelay = true };
            await client.ConnectAsync(devices.SednaHost, devices.SednaPort, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes(command + "\n"), timeout.Token);
            await stream.FlushAsync(timeout.Token);
            Connected = true;
            Fault = null;
            LastCommand = command;
            log.LogInformation("LED: {Command}", command);
        }
        catch (Exception e) when (e is SocketException or IOException or OperationCanceledException && !stop.IsCancellationRequested)
        {
            Connected = false;
            Fault = e.Message;
            throw new InvalidOperationException($"sedna {devices.SednaHost}:{devices.SednaPort} nicht erreichbar: {e.Message}", e);
        }
    }
}
