using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Clocks;

public sealed record ClockFinding(
    ClockTransport Transport, int Port, string Source, string ProtocolKey, string ProtocolName,
    string Encoding, string Sample, ClockState? Preview, int Score, int Packets);

public sealed record ScanResult(IReadOnlyList<ClockFinding> Findings, IReadOnlyList<int> BlockedPorts);

public sealed record HostFinding(string Address, long RoundTrip, string Name);

public sealed class ClockScanner(ClockFeed feed, ClockProtocols protocols, ILogger<ClockScanner> log)
{
    public static readonly int[] UdpPorts =
    [
        50085, 50086, 50087, 1024, 2000, 3000, 4000, 5000, 6000, 7000, 7001,
        8000, 9000, 9001, 10000, 10001, 12345, 20000, 30000, 50000,
    ];

    public static readonly int[] TcpPorts = [4001, 4000, 5000, 6000, 10001];

    public Task<ScanResult> ScanAsync(IEnumerable<int> udpPorts, IEnumerable<int> tcpPorts, TimeSpan duration, CancellationToken stop) =>
        feed.ExclusiveAsync(token => Collect(udpPorts.Distinct().ToArray(), tcpPorts.Distinct().ToArray(), duration, token), stop);

    async Task<ScanResult> Collect(int[] udpPorts, int[] tcpPorts, TimeSpan duration, CancellationToken stop)
    {
        var samples = new ConcurrentDictionary<(ClockTransport Transport, int Port, string Source), Sample>();
        var blocked = new List<int>();
        var sockets = new List<UdpClient>();
        var listeners = new List<TcpListener>();

        foreach (var port in udpPorts)
        {
            try { sockets.Add(BindUdp(port)); }
            catch (SocketException) { blocked.Add(port); }
        }

        foreach (var port in tcpPorts)
        {
            try { listeners.Add(BindTcp(port)); }
            catch (SocketException) { blocked.Add(port); }
        }

        log.LogInformation("Suchlauf auf {Udp} UDP- und {Tcp} TCP-Ports für {Seconds} s",
            sockets.Count, listeners.Count, duration.TotalSeconds);

        using var window = CancellationTokenSource.CreateLinkedTokenSource(stop);
        window.CancelAfter(duration);

        try
        {
            await Task.WhenAll(
                sockets.Select(socket => DrainUdp(socket, samples, window.Token))
                    .Concat(listeners.Select(listener => DrainTcp(listener, samples, window.Token))));
        }
        finally
        {
            foreach (var socket in sockets) socket.Dispose();
            foreach (var listener in listeners) listener.Stop();
        }

        var findings = samples
            .Select(entry => Classify(entry.Key.Transport, entry.Key.Port, entry.Key.Source, entry.Value))
            .OrderByDescending(finding => finding.Score)
            .ThenByDescending(finding => finding.Packets)
            .ToList();

        return new ScanResult(findings, blocked);
    }

    static async Task DrainUdp(UdpClient socket, ConcurrentDictionary<(ClockTransport, int, string), Sample> samples, CancellationToken stop)
    {
        var port = ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await socket.ReceiveAsync(stop); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }

            Remember(samples, ClockTransport.Udp, port, result.RemoteEndPoint.Address.ToString(), result.Buffer);
        }
    }

    static async Task DrainTcp(TcpListener listener, ConcurrentDictionary<(ClockTransport, int, string), Sample> samples, CancellationToken stop)
    {
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        while (!stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(stop); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }

            using (client)
            {
                var source = ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address.ToString() ?? "unbekannt";
                var buffer = new byte[4096];
                await using var stream = client.GetStream();
                while (!stop.IsCancellationRequested)
                {
                    int read;
                    try { read = await stream.ReadAsync(buffer, stop); }
                    catch (Exception) { break; }
                    if (read == 0) break;
                    Remember(samples, ClockTransport.Tcp, port, source, buffer[..read]);
                }
            }
        }
    }

    static void Remember(ConcurrentDictionary<(ClockTransport, int, string), Sample> samples,
        ClockTransport transport, int port, string source, byte[] payload)
    {
        if (payload.Length == 0) return;
        samples.AddOrUpdate((transport, port, source),
            new Sample(payload, 1),
            (_, existing) => new Sample(payload, existing.Packets + 1));
    }

    ClockFinding Classify(ClockTransport transport, int port, string source, Sample sample)
    {
        var best = protocols.Best(sample.Payload);
        var protocol = best?.Protocol;
        var encoding = LineDecoder.Detect(sample.Payload);
        var config = new ClockSourceConfig
        {
            Port = port,
            SourceIp = source,
            Encoding = encoding,
            Protocol = protocol?.Key ?? DelimitedProtocol.Id,
        };

        var parser = protocol?.CreateParser(config);
        return new ClockFinding(
            transport, port, source,
            protocol?.Key ?? "",
            protocol?.Name ?? "unbekannt",
            encoding,
            parser?.Describe(sample.Payload) ?? ClockFieldReader.Hex(sample.Payload),
            parser?.Read(sample.Payload),
            best?.Score ?? 0,
            sample.Packets);
    }

    static UdpClient BindUdp(int port)
    {
        var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        return udp;
    }

    static TcpListener BindTcp(int port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();
        return listener;
    }

    public static async Task<IReadOnlyList<HostFinding>> SweepAsync(CancellationToken stop)
    {
        var local = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.PrefixLength >= 24)
            .Select(a => a.Address.GetAddressBytes())
            .ToList();

        var targets = local
            .SelectMany(bytes => Enumerable.Range(1, 254).Select(host => new IPAddress([bytes[0], bytes[1], bytes[2], (byte)host])))
            .Distinct()
            .ToList();

        var found = new ConcurrentBag<HostFinding>();
        await Parallel.ForEachAsync(targets, new ParallelOptions { MaxDegreeOfParallelism = 64, CancellationToken = stop }, async (address, token) =>
        {
            using var ping = new Ping();
            try
            {
                var reply = await ping.SendPingAsync(address, TimeSpan.FromMilliseconds(600), cancellationToken: token);
                if (reply.Status == IPStatus.Success) found.Add(new HostFinding(address.ToString(), reply.RoundtripTime, await NameOf(address)));
            }
            catch (Exception) { }
        });

        return found.OrderBy(host => host.Address.Split('.').Select(int.Parse).ToArray(), new OctetComparer()).ToList();
    }

    static async Task<string> NameOf(IPAddress address)
    {
        try { return (await Dns.GetHostEntryAsync(address)).HostName; }
        catch (Exception) { return ""; }
    }

    sealed record Sample(byte[] Payload, int Packets);

    sealed class OctetComparer : IComparer<int[]>
    {
        public int Compare(int[]? left, int[]? right)
        {
            if (left is null || right is null) return 0;
            for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
                if (left[i] != right[i]) return left[i].CompareTo(right[i]);
            return left.Length.CompareTo(right.Length);
        }
    }
}
