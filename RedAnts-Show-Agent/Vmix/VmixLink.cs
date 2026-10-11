using System.Threading.Channels;
using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Vmix;

public sealed class VmixLink(PayloadStore payload, LiveGame live, ILogger<VmixLink> log) : BackgroundService
{
    const int OverlayChannels = 4;

    readonly Channel<ClockState> _pending = Channel.CreateBounded<ClockState>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    readonly Dictionary<string, string> _sent = new(StringComparer.OrdinalIgnoreCase);
    readonly SemaphoreSlim _send = new(1, 1);

    VmixConnection? _connection;
    CancellationTokenSource? _session;
    bool _refresh;

    public bool Configured => payload.Hall.Devices.VmixHost.Length > 0;

    public bool Connected => _connection?.Connected ?? false;

    public string? Fault { get; private set; }

    public string? LiveTitle { get; private set; }

    public long Commands { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        live.Changed += Queue;
        payload.Changed += Restart;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var session = CancellationTokenSource.CreateLinkedTokenSource(stop);
                _session = session;
                var devices = payload.Hall.Devices;

                if (devices.VmixHost.Length == 0)
                {
                    Fault = null;
                    try { await Task.Delay(Timeout.InfiniteTimeSpan, session.Token); }
                    catch (OperationCanceledException) { }
                    continue;
                }

                try { await PumpAsync(devices, session.Token); }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Fault = e.Message;
                    log.LogWarning("vMix {Host}:{Port} nicht erreichbar: {Message}", devices.VmixHost, devices.VmixPort, e.Message);
                    try { await Task.Delay(TimeSpan.FromSeconds(5), stop); }
                    catch (OperationCanceledException) { break; }
                }
                finally { _connection = null; }
            }
        }
        finally
        {
            live.Changed -= Queue;
            payload.Changed -= Restart;
        }
    }

    void Queue() => _pending.Writer.TryWrite(live.Clock);

    void Restart()
    {
        _sent.Clear();
        _session?.Cancel();
    }

    async Task PumpAsync(HallDevices devices, CancellationToken stop)
    {
        await using var connection = await VmixConnection.OpenAsync(devices.VmixHost, devices.VmixPort, stop);
        _connection = connection;
        Fault = null;
        _sent.Clear();
        _refresh = true;
        log.LogInformation("vMix {Host}:{Port} verbunden", devices.VmixHost, devices.VmixPort);

        using var session = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var drain = Task.Run(async () =>
        {
            try { await connection.DrainAsync(session.Token); }
            catch (Exception e) when (e is not OperationCanceledException) { Fault = e.Message; }
            finally { session.Cancel(); }
        }, stop);

        try
        {
            await PublishScoreboardAsync(live.Clock, session.Token);
            while (!session.IsCancellationRequested)
            {
                var state = await _pending.Reader.ReadAsync(session.Token);
                await PublishScoreboardAsync(state, session.Token);
            }
        }
        catch (OperationCanceledException) when (!stop.IsCancellationRequested)
        {
            throw new IOException(Fault ?? "vMix hat die Verbindung geschlossen");
        }
        finally
        {
            session.Cancel();
            await drain;
        }
    }

    async Task PublishScoreboardAsync(ClockState state, CancellationToken stop)
    {
        if (!state.HasData) return;
        var title = payload.Hall.Title(VmixScoreboard.Kind);
        var input = title?.Input is { Length: > 0 } configured ? configured : VmixScoreboard.LuplInput;
        var fields = title?.Fields is { Count: > 0 } mapped ? mapped : VmixScoreboard.LuplFields;
        var game = payload.Game;
        var values = VmixScoreboard.Values(state,
            game.Home.LongName.Length > 0 ? game.Home.LongName : state.Home,
            game.Away.LongName.Length > 0 ? game.Away.LongName : state.Guest);

        var commands = new List<string>();
        foreach (var (slot, value) in values)
        {
            if (!fields.TryGetValue(slot, out var field) || field.Length == 0) continue;
            foreach (var (name, text) in VmixScoreboard.Expand(field, value))
            {
                var key = $"{input}|{name}";
                if (_sent.TryGetValue(key, out var previous) && previous == text) continue;
                _sent[key] = text;
                commands.Add(VmixConnection.Function(VmixScoreboard.FunctionFor(name), ("Input", input), ("SelectedName", name), ("Value", text)));
            }
        }
        if (commands.Count == 0) return;

        if (_refresh)
        {
            commands.Insert(0, VmixConnection.Function("PauseRender", ("Input", input)));
            commands.Add(VmixConnection.Function("ResumeRender", ("Input", input)));
            _refresh = false;
        }
        await SendAsync(commands, stop);
    }

    public async Task ShowTitleAsync(OverlayTitle title, IReadOnlyDictionary<string, string> values, CancellationToken stop)
    {
        var commands = values
            .Select(kv => VmixConnection.Function(VmixScoreboard.FunctionFor(kv.Key), ("Input", title.Input), ("SelectedName", kv.Key), ("Value", kv.Value)))
            .ToList();
        commands.Add(VmixConnection.Function($"OverlayInput{title.Channel}In", ("Input", title.Input)));
        await SendAsync(commands, stop);
        LiveTitle = title.Kind;
    }

    public async Task HideTitleAsync(OverlayTitle title, CancellationToken stop)
    {
        await SendAsync([VmixConnection.Function($"OverlayInput{title.Channel}Out")], stop);
        if (LiveTitle == title.Kind) LiveTitle = null;
    }

    public async Task HideAllAsync(CancellationToken stop)
    {
        var channels = payload.Hall.OverlayTitles
            .Where(t => t.Kind != VmixScoreboard.Kind)
            .Select(t => t.Channel)
            .Where(c => c is >= 1 and <= OverlayChannels)
            .DefaultIfEmpty(1)
            .Distinct();
        await SendAsync(channels.Select(c => VmixConnection.Function($"OverlayInput{c}Out")).ToList(), stop);
        LiveTitle = null;
    }

    async Task SendAsync(IReadOnlyList<string> commands, CancellationToken stop)
    {
        var connection = _connection ?? throw new InvalidOperationException("vMix ist nicht verbunden");
        await _send.WaitAsync(stop);
        try
        {
            await connection.SendAsync(commands, stop);
            Commands += commands.Count;
        }
        finally { _send.Release(); }
    }
}
