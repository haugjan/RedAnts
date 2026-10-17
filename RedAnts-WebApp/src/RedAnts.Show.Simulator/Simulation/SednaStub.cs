using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace RedAnts.Show.Simulator.Simulation;

public sealed record SednaCommand(DateTime At, string Raw, string? Playlist, bool Overlay);

public sealed partial class SednaStub(EventLog log) : BackgroundService
{
    private readonly object _gate = new();
    private readonly List<SednaCommand> _commands = new();

    public int Port { get; set; } = 27182;
    public bool Listening { get; private set; }
    public string? Fault { get; private set; }
    public string? MainPlaylist { get; private set; }
    public string? OverlayPlaylist { get; private set; }

    public event Action? Changed;

    public IReadOnlyList<SednaCommand> LatestCommands(int count = 20)
    {
        lock (_gate) return _commands.AsEnumerable().Reverse().Take(count).ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            var listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                listener.Start();
                Listening = true;
                Fault = null;
                log.Add("LED", $"sedna-Stellvertreter hört auf TCP {Port}");
                Changed?.Invoke();
                while (!stop.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(stop);
                    _ = ServeAsync(client, stop);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException e)
            {
                Listening = false;
                Fault = e.Message;
                log.Add("LED", $"Port {Port} nicht verfügbar: {e.Message}");
                Changed?.Invoke();
                try { await Task.Delay(TimeSpan.FromSeconds(3), stop); } catch (OperationCanceledException) { break; }
            }
            finally { listener.Stop(); Listening = false; }
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken stop)
    {
        try
        {
            using (client)
            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!stop.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(stop);
                    if (line is null) break;
                    Handle(line.Trim());
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("OK\n"), stop);
                }
            }
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException) { }
    }

    private void Handle(string line)
    {
        if (line.Length == 0) return;
        var overlay = OverlayFlag().IsMatch(line);
        string? playlist = null;
        if (line.StartsWith("start playlist", StringComparison.OrdinalIgnoreCase))
        {
            playlist = PlaylistName().Match(line) is { Success: true } m ? m.Groups[1].Value : "?";
            if (overlay) OverlayPlaylist = playlist; else MainPlaylist = playlist;
        }
        else if (line.StartsWith("stop playlist", StringComparison.OrdinalIgnoreCase))
        {
            if (overlay) OverlayPlaylist = null; else { MainPlaylist = null; OverlayPlaylist = null; }
        }
        lock (_gate)
        {
            _commands.Add(new SednaCommand(DateTime.Now, line, playlist, overlay));
            if (_commands.Count > 500) _commands.RemoveRange(0, _commands.Count - 500);
        }
        log.Add("LED", line);
        Changed?.Invoke();
    }

    [GeneratedRegex("-n\\s+\"([^\"]*)\"")]
    private static partial Regex PlaylistName();

    [GeneratedRegex("(^|\\s)-o(\\s|$)")]
    private static partial Regex OverlayFlag();
}
