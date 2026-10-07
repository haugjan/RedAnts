using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Clocks;

public sealed class ClockFeed(
    ClockHub hub,
    ConfigStore store,
    ClockProtocols protocols,
    IOptions<GameClockOptions> options,
    IHostEnvironment env,
    ILogger<ClockFeed> log) : BackgroundService
{
    static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(1);

    readonly SemaphoreSlim _socket = new(1, 1);

    CancellationTokenSource? _session;
    volatile bool _yield;

    public string Listening { get; private set; } = "";

    public string? Fault { get; private set; }

    public long Received { get; private set; }

    public long Rejected { get; private set; }

    public string LastRaw { get; private set; } = "";

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        store.Changed += Restart;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var session = CancellationTokenSource.CreateLinkedTokenSource(stop);
                _session = session;
                try
                {
                    await ListenAsync(store.Current.Clock, session.Token);
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Fault = e.Message;
                    log.LogError("Empfang auf Port {Port} gescheitert: {Message}", store.Current.Clock.Port, e.Message);
                    try { await Task.Delay(TimeSpan.FromSeconds(3), stop); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        finally { store.Changed -= Restart; }
    }

    public async Task<T> ExclusiveAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken stop)
    {
        _yield = true;
        _session?.Cancel();
        await _socket.WaitAsync(stop);
        try { return await work(stop); }
        finally
        {
            _socket.Release();
            _yield = false;
            _session?.Cancel();
        }
    }

    void Restart()
    {
        hub.Reset();
        _session?.Cancel();
    }

    async Task ListenAsync(ClockSourceConfig config, CancellationToken stop)
    {
        while (_yield) await Task.Delay(100, stop);

        await _socket.WaitAsync(stop);
        try
        {
            var protocol = protocols.Find(config.Protocol);
            var source = config.AcceptsAnySource ? null : IPAddress.Parse(config.SourceIp!);
            var logDir = Path.Combine(env.ContentRootPath, options.Value.LogDir);
            Directory.CreateDirectory(logDir);

            using var udp = Bind(config.Port);
            Fault = null;
            Listening = $"{protocol.Name} auf UDP {config.Port} ({(source is null ? "alle Absender" : source.ToString())})";
            log.LogInformation("Hoere auf UDP-Port {Port} (Quelle: {Source}, Protokoll: {Protocol})",
                config.Port, source?.ToString() ?? "alle", protocol.Key);

            var lastRaw = "";
            var lastPublish = DateTime.MinValue;
            while (!stop.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try { result = await udp.ReceiveAsync(stop); }
                catch (SocketException) { continue; }

                if (source is not null && !result.RemoteEndPoint.Address.Equals(source)) continue;

                Received++;
                var raw = LineDecoder.Decode(result.Buffer, config.Encoding);
                LastRaw = raw;

                var changed = raw != lastRaw;
                if (!changed && DateTime.Now - lastPublish < Heartbeat) continue;

                if (protocol.Parse(raw, config) is not { } state)
                {
                    Rejected++;
                    lastRaw = raw;
                    continue;
                }

                hub.Publish(state);
                lastPublish = DateTime.Now;

                if (changed)
                {
                    lastRaw = raw;
                    await AppendRawLog(logDir, raw, stop);
                }
            }
        }
        finally
        {
            Listening = "";
            _socket.Release();
        }
    }

    static UdpClient Bind(int port)
    {
        var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        return udp;
    }

    static async Task AppendRawLog(string logDir, string raw, CancellationToken stop)
    {
        var file = Path.Combine(logDir, $"gameclock-{DateTime.Now:yyyy-MM-dd}.txt");
        try { await File.AppendAllTextAsync(file, $"{DateTime.Now:HH:mm:ss.fff} {raw}\n", stop); }
        catch (IOException) { }
    }
}
