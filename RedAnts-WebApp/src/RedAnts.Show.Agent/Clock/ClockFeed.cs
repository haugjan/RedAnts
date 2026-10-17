using System.Net;
using System.Net.Sockets;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Clock;

public sealed class ClockFeed(PayloadStore payload, LiveGame live, ILogger<ClockFeed> log) : BackgroundService
{
    static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(1);
    const int ReadBuffer = 4096;

    CancellationTokenSource? _session;
    IClockReader _reader = new ICastReader();
    string _lastRaw = "";
    DateTime _lastPublish = DateTime.MinValue;

    public string Listening { get; private set; } = "";

    public string? Fault { get; private set; }

    public long Received { get; private set; }

    public long Rejected { get; private set; }

    public long Foreign { get; private set; }

    public string LastRaw { get; private set; } = "";

    public bool Active => Listening.Length > 0 && Fault is null;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        payload.Changed += Restart;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var session = CancellationTokenSource.CreateLinkedTokenSource(stop);
                _session = session;
                var devices = payload.Hall.Devices;
                try
                {
                    if (!payload.Hall.Offers.Clock)
                    {
                        Fault = null;
                        await Task.Delay(Timeout.InfiniteTimeSpan, session.Token);
                    }
                    if (devices.ClockPort <= 0)
                    {
                        Fault = "Kein Port für die Matchuhr konfiguriert";
                        log.LogWarning("{Fault}", Fault);
                        await Task.Delay(Timeout.InfiniteTimeSpan, session.Token);
                    }
                    if (!ClockTypes.Supported(devices.ClockType))
                    {
                        Fault = $"Uhrtyp {devices.ClockType} wird vom Agenten noch nicht unterstützt";
                        log.LogWarning("{Fault}", Fault);
                        await Task.Delay(Timeout.InfiniteTimeSpan, session.Token);
                    }
                    await ListenAsync(devices, session.Token);
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Fault = e.Message;
                    log.LogError("Empfang der Matchuhr auf Port {Port} gescheitert: {Message}", devices.ClockPort, e.Message);
                    try { await Task.Delay(TimeSpan.FromSeconds(3), stop); }
                    catch (OperationCanceledException) { break; }
                }
                finally { Listening = ""; }
            }
        }
        finally { payload.Changed -= Restart; }
    }

    void Restart() => _session?.Cancel();

    static string Shorten(string raw) => raw.Length <= 120 ? raw : raw[..120] + "…";

    Task ListenAsync(HallDevices devices, CancellationToken stop)
    {
        _lastRaw = "";
        _lastPublish = DateTime.MinValue;
        Fault = null;
        _reader = ClockTypes.Reader(devices.ClockType);
        var type = ClockTypes.Name(devices.ClockType);
        var transport = ClockTypes.Transport(devices.ClockType);
        var from = devices.ClockSource.Length > 0 ? $" von {devices.ClockSource}" : "";
        Listening = $"{type} auf {(transport == ClockTransport.Tcp ? "TCP" : "UDP")} {devices.ClockPort}{from}";
        log.LogInformation("Höre auf die Matchuhr: {Listening}", Listening);

        return transport == ClockTransport.Tcp
            ? AcceptAsync(devices.ClockPort, devices.ClockSource, stop)
            : ReceiveAsync(devices.ClockPort, devices.ClockSource, stop);
    }

    async Task ReceiveAsync(int port, string source, CancellationToken stop)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(stop); }
            catch (SocketException) { continue; }
            if (Rejects(source, result.RemoteEndPoint.Address)) continue;
            Handle(result.Buffer);
        }
    }

    async Task AcceptAsync(int port, string source, CancellationToken stop)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(stop);
                if (client.Client.RemoteEndPoint is IPEndPoint remote && Rejects(source, remote.Address)) continue;
                log.LogInformation("Matchuhr verbunden: {Remote}", client.Client.RemoteEndPoint);
                _reader = ClockTypes.Reader(payload.Hall.Devices.ClockType);
                await ReadAsync(client, stop);
                log.LogInformation("Matchuhr getrennt");
            }
        }
        finally { listener.Stop(); }
    }

    async Task ReadAsync(TcpClient client, CancellationToken stop)
    {
        var buffer = new byte[ReadBuffer];
        var stream = client.GetStream();
        while (!stop.IsCancellationRequested)
        {
            int read;
            try { read = await stream.ReadAsync(buffer, stop); }
            catch (IOException) { return; }
            catch (SocketException) { return; }
            if (read <= 0) return;
            Handle(buffer[..read]);
        }
    }

    bool Rejects(string source, IPAddress address)
    {
        if (source.Length == 0 || address.ToString() == source) return false;
        Foreign++;
        return true;
    }

    void Handle(byte[] buffer)
    {
        if (buffer.Length == 0) return;
        Received++;
        LastRaw = _reader.Describe(buffer);

        if (_reader.Read(buffer) is not { } state)
        {
            Rejected++;
            if (Rejected % 50 == 1) log.LogWarning("Telegramm nicht lesbar: {Raw}", Shorten(LastRaw));
            return;
        }

        var changed = state.Raw != _lastRaw;
        if (!changed && DateTime.Now - _lastPublish < Heartbeat) return;

        live.Update(state);
        _lastPublish = DateTime.Now;
        if (changed) _lastRaw = state.Raw;
    }
}
