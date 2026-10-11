using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RedAnts.Show.Agent.Vmix;

public sealed class VmixConnection : IAsyncDisposable
{
    readonly TcpClient _client;
    readonly NetworkStream _stream;

    VmixConnection(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public bool Connected => _client.Connected;

    public static async Task<VmixConnection> OpenAsync(string host, int port, CancellationToken stop)
    {
        var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(host, port, stop);
        return new VmixConnection(client);
    }

    public async Task SendAsync(IEnumerable<string> commands, CancellationToken stop)
    {
        var payload = Encoding.UTF8.GetBytes(string.Concat(commands.Select(c => c + "\r\n")));
        if (payload.Length == 0) return;
        await _stream.WriteAsync(payload, stop);
        await _stream.FlushAsync(stop);
    }

    public async Task<IReadOnlyList<string>> ReadInputsAsync(CancellationToken stop)
    {
        await SendAsync(["XML"], stop);
        while (!stop.IsCancellationRequested)
        {
            var header = await ReadLineAsync(stop);
            var parts = header.Split(' ', 2);
            if (parts[0] != "XML") continue;
            if (parts.Length < 2 || !int.TryParse(parts[1], out var length))
                throw new InvalidOperationException($"vMix antwortet mit '{header}'");
            var xml = System.Text.Encoding.UTF8.GetString(await ReadExactlyAsync(length, stop));
            return System.Xml.Linq.XDocument.Parse(xml).Root?.Element("inputs")?.Elements("input")
                .Select(i => i.Attribute("title")?.Value ?? "")
                .Where(title => title.Length > 0)
                .ToList() ?? [];
        }
        return [];
    }

    async Task<byte[]> ReadExactlyAsync(int length, CancellationToken stop)
    {
        var buffer = new byte[length];
        var read = 0;
        while (read < length)
        {
            var chunk = await _stream.ReadAsync(buffer.AsMemory(read), stop);
            if (chunk == 0) throw new IOException("vMix hat die Verbindung geschlossen");
            read += chunk;
        }
        return buffer;
    }

    public async Task DrainAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
            await ReadLineAsync(stop);
    }

    async Task<string> ReadLineAsync(CancellationToken stop)
    {
        var line = new StringBuilder();
        var single = new byte[1];
        while (line.Length < 4096)
        {
            if (await _stream.ReadAsync(single, stop) == 0) throw new IOException("vMix hat die Verbindung geschlossen");
            if (single[0] == (byte)'\n') return line.ToString().TrimEnd('\r');
            line.Append((char)single[0]);
        }
        return line.ToString();
    }

    public static string Function(string name, params (string Key, string Value)[] args) =>
        $"FUNCTION {name}" + (args.Length == 0 ? "" : " " + string.Join('&', args.Select(a => $"{a.Key}={WebUtility.UrlEncode(a.Value)}")));

    public async ValueTask DisposeAsync()
    {
        try { await SendAsync(["QUIT"], CancellationToken.None); }
        catch (Exception) { }
        _stream.Dispose();
        _client.Dispose();
    }
}
