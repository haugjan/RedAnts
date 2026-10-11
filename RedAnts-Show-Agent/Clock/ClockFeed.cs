using System.Net;
using System.Net.Sockets;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Clock;

public sealed class ClockFeed(PayloadStore payload, LiveGame live, ILogger<ClockFeed> log) : BackgroundService
{
    static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(1);

    CancellationTokenSource? _session;
    string _lastRaw = "";
    DateTime _lastPublish = DateTime.MinValue;

    public string Listening { get; private set; } = "";

    public string? Fault { get; private set; }

    public long Received { get; private set; }

    public long Rejected { get; private set; }

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
                    if (!ICastProtocol.Supports(devices.ClockType))
                    {
                        Fault = $"Uhrtyp {devices.ClockType} wird vom Agenten noch nicht unterstützt";
                        log.LogWarning("{Fault}", Fault);
                        await Task.Delay(Timeout.InfiniteTimeSpan, session.Token);
                    }
                    await ListenAsync(devices.ClockPort, session.Token);
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

    async Task ListenAsync(int port, CancellationToken stop)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        _lastRaw = "";
        _lastPublish = DateTime.MinValue;
        Fault = null;
        Listening = $"iCast auf UDP {port}";
        log.LogInformation("Höre auf die Matchuhr: {Listening}", Listening);

        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(stop); }
            catch (SocketException) { continue; }
            Handle(result.Buffer);
        }
    }

    void Handle(byte[] buffer)
    {
        if (buffer.Length == 0) return;
        Received++;
        var raw = LineDecoder.Decode(buffer);
        LastRaw = raw;

        if (ICastProtocol.Parse(raw) is not { } state)
        {
            Rejected++;
            return;
        }

        var changed = raw != _lastRaw;
        if (!changed && DateTime.Now - _lastPublish < Heartbeat) return;

        live.Update(state);
        _lastPublish = DateTime.Now;
        if (changed) _lastRaw = raw;
    }
}
