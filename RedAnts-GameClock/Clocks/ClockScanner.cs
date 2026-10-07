using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RedAnts.GameClock.Clocks;

public sealed record ClockFinding(
    int Port, string Source, string ProtocolKey, string ProtocolName,
    string Encoding, string Sample, string[] Fields, ClockState? Preview, int Score, int Datagrams);

public sealed record ScanResult(IReadOnlyList<ClockFinding> Findings, IReadOnlyList<int> BlockedPorts);

public sealed record HostFinding(string Address, long RoundTrip, string Name);

public sealed class ClockScanner(ClockFeed feed, ClockProtocols protocols, ILogger<ClockScanner> log)
{
    public static readonly int[] CandidatePorts =
    [
        50085, 50086, 50087, 1024, 2000, 3000, 4000, 5000, 6000, 7000, 7001,
        8000, 9000, 9001, 10000, 10001, 12345, 20000, 30000, 50000,
    ];

    public Task<ScanResult> ScanAsync(IEnumerable<int> ports, TimeSpan duration, CancellationToken stop) =>
        feed.ExclusiveAsync(token => Collect(ports.Distinct().ToArray(), duration, token), stop);

    async Task<ScanResult> Collect(int[] ports, TimeSpan duration, CancellationToken stop)
    {
        var samples = new ConcurrentDictionary<(int Port, string Source), (string Raw, string Encoding, int Count)>();
        var blocked = new List<int>();
        var sockets = new List<UdpClient>();

        foreach (var port in ports)
        {
            try { sockets.Add(Bind(port)); }
            catch (SocketException) { blocked.Add(port); }
        }

        log.LogInformation("Suchlauf auf {Count} Ports für {Seconds} s", sockets.Count, duration.TotalSeconds);

        using var window = CancellationTokenSource.CreateLinkedTokenSource(stop);
        window.CancelAfter(duration);

        try
        {
            await Task.WhenAll(sockets.Select(socket => Drain(socket, samples, window.Token)));
        }
        finally
        {
            foreach (var socket in sockets) socket.Dispose();
        }

        var findings = samples
            .Select(entry => Classify(entry.Key.Port, entry.Key.Source, entry.Value.Raw, entry.Value.Encoding, entry.Value.Count))
            .OrderByDescending(f => f.Score)
            .ThenByDescending(f => f.Datagrams)
            .ToList();

        return new ScanResult(findings, blocked);
    }

    async Task Drain(UdpClient socket, ConcurrentDictionary<(int, string), (string, string, int)> samples, CancellationToken stop)
    {
        var port = ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await socket.ReceiveAsync(stop); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }

            if (result.Buffer.Length == 0) continue;
            var encoding = LineDecoder.Detect(result.Buffer);
            var raw = LineDecoder.Decode(result.Buffer, encoding);
            if (raw.Length == 0) continue;

            var key = (port, result.RemoteEndPoint.Address.ToString());
            samples.AddOrUpdate(key, (raw, encoding, 1), (_, existing) => (raw, encoding, existing.Item3 + 1));
        }
    }

    ClockFinding Classify(int port, string source, string raw, string encoding, int count)
    {
        var best = protocols.Best(raw);
        var protocol = best?.Protocol;
        var preview = protocol?.Parse(raw, Probe(port, source, encoding, protocol.Key));
        return new ClockFinding(
            port, source,
            protocol?.Key ?? "",
            protocol?.Name ?? "unbekannt",
            encoding, raw,
            raw.Split(';'),
            preview,
            best?.Score ?? 0,
            count);
    }

    static Configuration.ClockSourceConfig Probe(int port, string source, string encoding, string protocol) =>
        new() { Port = port, SourceIp = source, Encoding = encoding, Protocol = protocol };

    static UdpClient Bind(int port)
    {
        var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        return udp;
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

        return found.OrderBy(h => h.Address.Split('.').Select(int.Parse).ToArray(), new OctetComparer()).ToList();
    }

    static async Task<string> NameOf(IPAddress address)
    {
        try { return (await Dns.GetHostEntryAsync(address)).HostName; }
        catch (Exception) { return ""; }
    }

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
