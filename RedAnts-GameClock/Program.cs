using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Services.AddSingleton<ClockHub>();
builder.Services.AddHostedService<UdpReceiver>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

app.MapGet("/api/state", (ClockHub hub) => hub.Current);

app.MapGet("/events", async (HttpContext ctx, ClockHub hub) =>
{
    ctx.Response.Headers.ContentType = "text/event-stream";
    ctx.Response.Headers.CacheControl = "no-cache";
    var reader = hub.Subscribe(out var id);
    try
    {
        await WriteEvent(ctx, hub.Current);
        await foreach (var state in reader.ReadAllAsync(ctx.RequestAborted))
            await WriteEvent(ctx, state);
    }
    catch (OperationCanceledException) { }
    finally { hub.Unsubscribe(id); }
});

app.Lifetime.ApplicationStarted.Register(() => PrintUrls(app));
app.Run();

async Task WriteEvent(HttpContext ctx, ClockState state)
{
    await ctx.Response.WriteAsync($"data: {JsonSerializer.Serialize(state, jsonOptions)}\n\n");
    await ctx.Response.Body.FlushAsync();
}

static void PrintUrls(WebApplication app)
{
    var ports = app.Urls.Select(u => new Uri(u.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost").Replace("*", "localhost")).Port).Distinct();
    var addresses = NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(a => a.Address.ToString());

    Console.WriteLine();
    Console.WriteLine("  Red Ants Matchuhr laeuft:");
    foreach (var port in ports)
    {
        Console.WriteLine($"    http://localhost:{port}/");
        foreach (var address in addresses) Console.WriteLine($"    http://{address}:{port}/");
    }
    Console.WriteLine("  Beenden mit Ctrl+C");
    Console.WriteLine();
}

record ClockState(
    string Time, string HomeScore, string GuestScore, string Period,
    string Home, string Guest, string Mode, string[] Fields, string Raw, DateTime Received)
{
    public static readonly ClockState Empty = new("--:--", "-", "-", "", "", "", "", [], "", DateTime.MinValue);

    public static ClockState? Parse(string raw)
    {
        var f = raw.Split(';').Select(x => x.Trim()).ToArray();
        if (f.Length < 13) return null;
        return new ClockState(f[0], f[1], f[2], f[3], f[10], f[11], f[12], f, raw, DateTime.Now);
    }
}

class ClockHub
{
    readonly object _lock = new();
    readonly Dictionary<int, Channel<ClockState>> _subscribers = new();
    int _next;

    public ClockState Current { get; private set; } = ClockState.Empty;

    public ChannelReader<ClockState> Subscribe(out int id)
    {
        var channel = Channel.CreateBounded<ClockState>(new BoundedChannelOptions(10) { FullMode = BoundedChannelFullMode.DropOldest });
        lock (_lock) { id = _next++; _subscribers[id] = channel; }
        return channel.Reader;
    }

    public void Unsubscribe(int id) { lock (_lock) _subscribers.Remove(id); }

    public void Publish(ClockState state)
    {
        lock (_lock)
        {
            Current = state;
            foreach (var channel in _subscribers.Values) channel.Writer.TryWrite(state);
        }
    }
}

class UdpReceiver(ClockHub hub, IConfiguration config, IHostEnvironment env, ILogger<UdpReceiver> log) : BackgroundService
{
    static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        int port = config.GetValue("GameClock:Port", 50085);
        string? ip = config.GetValue<string>("GameClock:Ip");
        IPAddress? source = string.IsNullOrWhiteSpace(ip) || ip == "any" ? null : IPAddress.Parse(ip);
        string logDir = Path.Combine(env.ContentRootPath, config.GetValue("GameClock:LogDir", "logs")!);
        Directory.CreateDirectory(logDir);

        var decoder = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        log.LogInformation("Hoere auf UDP-Port {Port} (Quelle: {Source})", port, source?.ToString() ?? "alle");

        string lastRaw = "";
        DateTime lastPublish = DateTime.MinValue;
        while (!stop.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(stop); }
            catch (OperationCanceledException) { break; }
            catch (SocketException) { continue; }

            if (source != null && !result.RemoteEndPoint.Address.Equals(source)) continue;

            string raw = decoder.GetString(result.Buffer).TrimEnd('\0');
            bool changed = raw != lastRaw;
            if (!changed && DateTime.Now - lastPublish < Heartbeat) continue;
            if (ClockState.Parse(raw) is not { } state) continue;

            hub.Publish(state);
            lastPublish = DateTime.Now;

            if (changed)
            {
                lastRaw = raw;
                await AppendRawLog(logDir, raw, stop);
            }
        }
    }

    static async Task AppendRawLog(string logDir, string raw, CancellationToken stop)
    {
        string file = Path.Combine(logDir, $"gameclock-{DateTime.Now:yyyy-MM-dd}.txt");
        try { await File.AppendAllTextAsync(file, $"{DateTime.Now:HH:mm:ss.fff} {raw}\n", stop); }
        catch (IOException) { }
    }
}
