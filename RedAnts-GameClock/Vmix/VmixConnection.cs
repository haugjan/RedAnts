using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace RedAnts.GameClock.Vmix;

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

    public async Task<XDocument> ReadStateAsync(CancellationToken stop)
    {
        await SendAsync(["XML"], stop);
        while (true)
        {
            var header = await ReadLineAsync(stop);
            var parts = header.Split(' ', 2);
            if (parts[0] != "XML") continue;
            if (parts.Length < 2 || !int.TryParse(parts[1], out var length))
                throw new InvalidOperationException($"vMix antwortet mit '{header}'");
            return XDocument.Parse(Encoding.UTF8.GetString(await ReadExactlyAsync(length, stop)));
        }
    }

    async Task<string> ReadLineAsync(CancellationToken stop)
    {
        var line = new StringBuilder();
        var single = new byte[1];
        while (line.Length < 1024)
        {
            if (await _stream.ReadAsync(single, stop) == 0) throw new IOException("vMix hat die Verbindung geschlossen");
            if (single[0] == (byte)'\n') return line.ToString().TrimEnd('\r');
            line.Append((char)single[0]);
        }
        throw new InvalidOperationException("vMix antwortet mit einer überlangen Zeile");
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

    public async ValueTask DisposeAsync()
    {
        try { await SendAsync(["QUIT"], CancellationToken.None); }
        catch (Exception) { }
        _stream.Dispose();
        _client.Dispose();
    }
}

public sealed record VmixInput(string Title, string Number, string Type, IReadOnlyList<string> TextFields, IReadOnlyList<string> ImageFields)
{
    public static IReadOnlyList<VmixInput> From(XDocument state) =>
        state.Root?.Element("inputs")?.Elements("input").Select(input => new VmixInput(
            input.Attribute("title")?.Value ?? "",
            input.Attribute("number")?.Value ?? "",
            input.Attribute("type")?.Value ?? "",
            Names(input, "text"),
            Names(input, "image"))).ToList() ?? [];

    static IReadOnlyList<string> Names(XElement input, string element) =>
        input.Elements(element)
            .Select(e => e.Attribute("name")?.Value ?? "")
            .Where(name => name.Length > 0)
            .ToList();
}
