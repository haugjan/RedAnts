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

    public bool Configured => payload.Hall.Offers.Vmix && payload.Hall.Devices.VmixHost.Length > 0;

    public bool Connected => _connection?.Connected ?? false;

    public string? Fault { get; private set; }

    public string? LiveTitle { get; private set; }

    public IReadOnlyList<VmixInput> Inputs { get; private set; } = [];

    public event Action<IReadOnlyList<VmixInput>>? InputsRead;

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

                if (!payload.Hall.Offers.Vmix || devices.VmixHost.Length == 0)
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

        try
        {
            Inputs = await connection.ReadInputsAsync(stop);
            log.LogInformation("vMix meldet {Count} Inputs", Inputs.Count);
            InputsRead?.Invoke(Inputs);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogWarning("vMix-Inputs nicht lesbar: {Message}", e.Message);
        }

        using var session = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var drain = Task.Run(async () =>
        {
            try { await connection.DrainAsync(Answer, session.Token); }
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

    void Answer(string line)
    {
        if (!line.StartsWith("ER", StringComparison.OrdinalIgnoreCase)) return;
        Fault = line;
        log.LogWarning("vMix weist einen Befehl zurueck: {Answer}", line);
    }

    async Task PublishScoreboardAsync(ClockState state, CancellationToken stop)
    {
        var title = payload.Hall.Title(VmixScoreboard.Kind);
        var input = title?.Input is { Length: > 0 } configured ? configured : VmixScoreboard.LuplInput;
        var fields = title?.Fields is { Count: > 0 } mapped ? mapped : VmixScoreboard.LuplFields;
        var game = payload.Game;
        var folder = payload.Hall.Devices.ImageFolder;
        var home = Name(game.Home, state.Home);
        var away = Name(game.Away, state.Guest);
        var values = state.HasData
            ? VmixScoreboard.Values(state, home, away)
            : VmixScoreboard.Manual(live.HomeScore, live.AwayScore, home, away);
        if (folder.Length > 0)
        {
            values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
            {
                ["homeLogo"] = VmixScoreboard.LogoPath(folder, home),
                ["awayLogo"] = VmixScoreboard.LogoPath(folder, away),
            };
        }

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
        var scoreboard = title.Kind.Equals(VmixScoreboard.Kind, StringComparison.OrdinalIgnoreCase);
        var commands = scoreboard
            ? new List<string>()
            : values
                .Select(kv => VmixConnection.Function(VmixScoreboard.FunctionFor(kv.Key), ("Input", title.Input), ("SelectedName", kv.Key), ("Value", kv.Value)))
                .ToList();
        if (Overlay(title) is { } channel) commands.Add(VmixConnection.Function($"OverlayInput{channel}In", ("Input", title.Input)));
        else if (scoreboard) throw new InvalidOperationException("Die Uhr hat keinen Overlay-Kanal (vMix-Titel, Kanal 1 bis 4)");
        await SendAsync(commands, stop);
        LiveTitle = title.Kind;
    }

    public async Task HideTitleAsync(OverlayTitle title, CancellationToken stop)
    {
        if (Overlay(title) is not { } channel)
            throw new InvalidOperationException("Die Uhr hat keinen Overlay-Kanal (vMix-Titel, Kanal 1 bis 4)");
        await SendAsync([VmixConnection.Function($"OverlayInput{channel}Out")], stop);
        if (LiveTitle == title.Kind) LiveTitle = null;
    }

    static int? Overlay(OverlayTitle title) => title.Channel is >= 1 and <= OverlayChannels ? title.Channel : null;

    static string Name(GameTeam team, string fallback) =>
        team.ShortName is { Length: > 0 } name ? name : fallback;

    public async Task HideAllAsync(CancellationToken stop) => await HideAllAsync(false, stop);

    public async Task HideAllAsync(bool withClock, CancellationToken stop)
    {
        var channels = payload.Hall.OverlayTitles
            .Where(t => withClock || t.Kind != VmixScoreboard.Kind)
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
