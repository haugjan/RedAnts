using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace RedAnts.GameClock;

public sealed class UdpReceiver(ClockHub hub, IOptions<GameClockOptions> options, IHostEnvironment env, ILogger<UdpReceiver> log) : BackgroundService
{
    static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(1);
    static readonly UTF32Encoding Decoder = new(bigEndian: true, byteOrderMark: false);

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var settings = options.Value;
        IPAddress? source = string.IsNullOrWhiteSpace(settings.Ip) || settings.Ip == "any" ? null : IPAddress.Parse(settings.Ip);
        string logDir = Path.Combine(env.ContentRootPath, settings.LogDir);
        Directory.CreateDirectory(logDir);

        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, settings.Port));
        log.LogInformation("Hoere auf UDP-Port {Port} (Quelle: {Source})", settings.Port, source?.ToString() ?? "alle");

        string lastRaw = "";
        DateTime lastPublish = DateTime.MinValue;
        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(stop); }
            catch (OperationCanceledException) { break; }
            catch (SocketException) { continue; }

            if (source != null && !result.RemoteEndPoint.Address.Equals(source)) continue;

            string raw = Decoder.GetString(result.Buffer).TrimEnd('\0');
            bool changed = raw != lastRaw;
            if (!changed && DateTime.Now - lastPublish < Heartbeat) continue;
            if (ClockState.Parse(raw) is not { } state) continue;

            hub.Publish(state);
            lastPublish = DateTime.Now;

            if (changed)
            {
                lastRaw = raw;
                await AppendRawLog(logDir, raw, stop);
            }
        }
    }

    static async Task AppendRawLog(string logDir, string raw, CancellationToken stop)
    {
        string file = Path.Combine(logDir, $"gameclock-{DateTime.Now:yyyy-MM-dd}.txt");
        try { await File.AppendAllTextAsync(file, $"{DateTime.Now:HH:mm:ss.fff} {raw}\n", stop); }
        catch (IOException) { }
    }
}
