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
