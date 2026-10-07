using System.Net;
using System.Threading.Channels;
using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Configuration;
using RedAnts.GameClock.Teams;

namespace RedAnts.GameClock.Vmix;

public sealed class VmixPublisher(ClockHub hub, ConfigStore store, TeamResolver teams, ILogger<VmixPublisher> log) : BackgroundService
{
    readonly Channel<ClockState> _pending = Channel.CreateBounded<ClockState>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    readonly Dictionary<string, string> _sent = new(StringComparer.OrdinalIgnoreCase);

    CancellationTokenSource? _session;
    bool _refresh;

    public bool Enabled => store.Current.Vmix.Enabled;

    public bool Connected { get; private set; }

    public string? Fault { get; private set; }

    public DateTime? LastSent { get; private set; }

    public long Commands { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        hub.Changed += Queue;
        store.Changed += Restart;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                using var session = CancellationTokenSource.CreateLinkedTokenSource(stop);
                _session = session;
                var config = store.Current.Vmix;

                if (!config.Enabled)
                {
                    Connected = false;
                    Fault = null;
                    try { await Task.Delay(Timeout.InfiniteTimeSpan, session.Token); }
                    catch (OperationCanceledException) { }
                    continue;
                }

                try { await PumpAsync(config, session.Token); }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Fault = e.Message;
                    log.LogWarning("vMix {Host}:{Port} nicht erreichbar: {Message}", config.Host, config.Port, e.Message);
                    try { await Task.Delay(TimeSpan.FromSeconds(5), stop); }
                    catch (OperationCanceledException) { break; }
                }
                finally { Connected = false; }
            }
        }
        finally
        {
            hub.Changed -= Queue;
            store.Changed -= Restart;
        }
    }

    void Queue(ClockState state) => _pending.Writer.TryWrite(state);

    void Restart()
    {
        _sent.Clear();
        _session?.Cancel();
    }

    async Task PumpAsync(VmixConfig config, CancellationToken stop)
    {
        await using var connection = await VmixConnection.OpenAsync(config.Host, config.Port, stop);
        Connected = true;
        Fault = null;
        _sent.Clear();
        _refresh = true;
        log.LogInformation("vMix {Host}:{Port} verbunden, Titel {Input}", config.Host, config.Port, config.Input);

        await SendAsync(connection, config, hub.Current, stop);

        while (!stop.IsCancellationRequested)
        {
            var state = await _pending.Reader.ReadAsync(stop);
            await SendAsync(connection, config, state, stop);
        }
    }

    async Task SendAsync(VmixConnection connection, VmixConfig config, ClockState state, CancellationToken stop)
    {
        if (!state.HasData || config.Input.Length == 0) return;

        var home = teams.Home(state);
        var guest = teams.Guest(state);
        var scene = new VmixScene(
            home.Name, guest.Name,
            config.SendLogos ? teams.LocalLogoPath(home) : "",
            config.SendLogos ? teams.LocalLogoPath(guest) : "",
            config.PenaltyFill);

        var payload = VmixPayload.Build(state, scene);
        var commands = new List<string>();

        Collect(commands, config, "SetText", payload.Text);
        Collect(commands, config, "SetColor", payload.Colors);
        if (config.SendLogos) Collect(commands, config, "SetImage", payload.Images);

        if (commands.Count == 0) return;

        if (_refresh)
        {
            commands.Insert(0, $"FUNCTION PauseRender Input={Escape(config.Input)}");
            commands.Add($"FUNCTION ResumeRender Input={Escape(config.Input)}");
            _refresh = false;
        }

        await connection.SendAsync(commands, stop);
        Commands += commands.Count;
        LastSent = DateTime.Now;
    }

    void Collect(List<string> commands, VmixConfig config, string function, IReadOnlyDictionary<string, string> values)
    {
        foreach (var (slot, value) in values)
        {
            if (!config.Fields.TryGetValue(slot, out var field) || field.Length == 0) continue;
            foreach (var (name, text) in VmixSlots.Expand(field, value))
            {
                if (_sent.TryGetValue(name, out var previous) && previous == text) continue;
                _sent[name] = text;
                commands.Add($"FUNCTION {function} Input={Escape(config.Input)}&SelectedName={Escape(name)}&Value={Escape(text)}");
            }
        }
    }

    static string Escape(string value) => WebUtility.UrlEncode(value) ?? "";
}
