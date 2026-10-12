using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Cloud;

public sealed class StatusReporter(
    CloudLink cloud,
    PayloadStore payload,
    LiveGame live,
    ShowStates states,
    ClockFeed clock,
    VmixLink vmix,
    SednaLink led,
    SceneRunner runner,
    Deck.Deck deck) : BackgroundService
{
    static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
    static readonly TimeSpan StatusRepeat = TimeSpan.FromSeconds(15);

    static readonly TimeSpan ClockPush = TimeSpan.FromMilliseconds(200);

    readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _sent = new();
    readonly object _clockGate = new();
    DateTime _clockPushedAt = DateTime.MinValue;
    DateTimeOffset _statusSentAt = DateTimeOffset.MinValue;
    IReadOnlyList<VmixInput> _inputs = [];

    public void ReportSound(SoundCommand sound) =>
        _ = cloud.ReportAsync("sound", new { action = SceneActions.SoundPlay, target = SceneActions.SoundPrefix + sound.Id, sound = sound.Id, label = sound.Label, scene = "deck" }, CancellationToken.None);

    public void ReportInputs(IReadOnlyList<VmixInput> inputs) => _inputs = inputs;

    public Dictionary<string, object?> Status()
    {
        var status = CloudStatus();
        status["clockReceived"] = clock.Received;
        status["clockRejected"] = clock.Rejected;
        status["clockForeign"] = clock.Foreign;
        status["clockLastRaw"] = clock.LastRaw;
        return status;
    }

    public Dictionary<string, object?> CloudStatus() => new()
    {
        ["vMix"] = vmix.Connected,
        ["Uhr"] = live.ClockLive,
        ["LED"] = led.Configured && led.Fault is null && led.Connected,
        ["version"] = AgentConfig.Version,
        ["hall"] = payload.Hall.Id,
        ["syncedAt"] = payload.Synced ? payload.Current.SentAt : null,
        ["game"] = payload.Game.Title,
        ["deck"] = deck.Context.ToString(),
        ["scene"] = runner.Running,
        ["vmixFault"] = vmix.Fault,
        ["clockListening"] = clock.Listening,
        ["clockFault"] = clock.Fault,
        ["ledFault"] = led.Fault,
        ["cloudFault"] = cloud.Fault,
        ["states"] = states.All,
    };

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        runner.Reported += Forward;
        live.Changed += PushClock;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                await SendAsync(stop);
                try { await Task.Delay(Tick, stop); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally
        {
            runner.Reported -= Forward;
            live.Changed -= PushClock;
        }
    }

    void Forward(string kind, object body)
    {
        if (kind is "overlay" or "sound" or "log") _ = cloud.ReportAsync(kind, body, CancellationToken.None);
    }

    async Task SendAsync(CancellationToken stop)
    {
        if (!cloud.Connected) return;

        var score = new { home = live.HomeScore, away = live.AwayScore, live = live.ClockLive };
        await SendIfChangedAsync("score", score, stop);

        states.Set(ShowStates.ClockRunning, live.ClockRunning ? "läuft" : "steht", "Matchuhr");
        await SendIfChangedAsync("clock", ClockBody(), stop);

        await SendIfChangedAsync("deck", new { context = deck.Context.ToString(), scene = runner.Running ?? "" }, stop);

        if (_inputs.Count > 0)
            await SendIfChangedAsync("vmix", new { inputs = _inputs.Select(i => new { title = i.Title, fields = i.Fields }) }, stop);

        var status = CloudStatus();
        var json = System.Text.Json.JsonSerializer.Serialize(status, PayloadJson.Options);
        var due = DateTimeOffset.Now - _statusSentAt > StatusRepeat;
        if (due || !_sent.TryGetValue("status", out var previous) || previous != json)
        {
            _sent["status"] = json;
            _statusSentAt = DateTimeOffset.Now;
            await cloud.ReportAsync("status", status, stop);
        }
    }

    void PushClock()
    {
        var now = DateTime.UtcNow;
        lock (_clockGate)
        {
            if (now - _clockPushedAt < ClockPush) return;
            _clockPushedAt = now;
        }
        _ = SendIfChangedAsync("clock", ClockBody(), CancellationToken.None);
    }

    object ClockBody() => new
    {
        time = live.Time,
        period = live.Period,
        periodLabel = live.PeriodLabel,
        mode = live.Clock.Mode,
        live = live.ClockLive,
    };

    async Task SendIfChangedAsync(string kind, object body, CancellationToken stop)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(body, PayloadJson.Options);
        if (_sent.TryGetValue(kind, out var previous) && previous == json) return;
        _sent[kind] = json;
        await cloud.ReportAsync(kind, body, stop);
    }
}
