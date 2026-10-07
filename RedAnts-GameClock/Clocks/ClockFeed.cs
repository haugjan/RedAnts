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
    string _lastRaw = "";
    DateTime _lastPublish = DateTime.MinValue;

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

            _lastRaw = "";
            _lastPublish = DateTime.MinValue;
            Fault = null;
            Listening = $"{protocol.Name} auf {protocol.Transport.ToString().ToUpperInvariant()} {config.Port} " +
                        $"({(source is null ? "alle Absender" : source.ToString())})";
            log.LogInformation("Hoere auf {Transport}-Port {Port} (Quelle: {Source}, Protokoll: {Protocol})",
                protocol.Transport, config.Port, source?.ToString() ?? "alle", protocol.Key);

            if (protocol.Transport == ClockTransport.Tcp)
                await ReceiveTcpAsync(config, protocol, source, logDir, stop);
            else
                await ReceiveUdpAsync(config, protocol, source, logDir, stop);
        }
        finally
        {
            Listening = "";
            _socket.Release();
        }
    }

    async Task ReceiveUdpAsync(ClockSourceConfig config, IClockProtocol protocol, IPAddress? source, string logDir, CancellationToken stop)
    {
        using var udp = BindUdp(config.Port);
        var parser = protocol.CreateParser(config);

        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(stop); }
            catch (SocketException) { continue; }

            if (source is not null && !result.RemoteEndPoint.Address.Equals(source)) continue;
            await Handle(result.Buffer, parser, logDir, stop);
        }
    }

    async Task ReceiveTcpAsync(ClockSourceConfig config, IClockProtocol protocol, IPAddress? source, string logDir, CancellationToken stop)
    {
        var listener = new TcpListener(IPAddress.Any, config.Port);
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(stop);
                var remote = (IPEndPoint?)client.Client.RemoteEndPoint;
                if (source is not null && remote is not null && !remote.Address.Equals(source)) continue;

                log.LogInformation("Matchuhr {Remote} verbunden", remote?.ToString() ?? "unbekannt");
                var parser = protocol.CreateParser(config);
                var buffer = new byte[4096];

                await using var stream = client.GetStream();
                while (!stop.IsCancellationRequested)
                {
                    int read;
                    try { read = await stream.ReadAsync(buffer, stop); }
                    catch (IOException) { break; }
                    if (read == 0) break;
                    await Handle(buffer[..read], parser, logDir, stop);
                }
            }
        }
        finally { listener.Stop(); }
    }

    async Task Handle(byte[] payload, IClockParser parser, string logDir, CancellationToken stop)
    {
        if (payload.Length == 0) return;

        Received++;
        var raw = parser.Describe(payload);
        LastRaw = raw;

        if (parser.Read(payload) is not { } state)
        {
            Rejected++;
            return;
        }

        var changed = raw != _lastRaw;
        if (!changed && DateTime.Now - _lastPublish < Heartbeat) return;

        hub.Publish(state);
        _lastPublish = DateTime.Now;

        if (changed)
        {
            _lastRaw = raw;
            await AppendRawLog(logDir, raw, stop);
        }
    }

    static UdpClient BindUdp(int port)
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
