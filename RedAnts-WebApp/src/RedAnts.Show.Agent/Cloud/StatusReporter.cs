using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Keys;
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
    Deck.Deck deck) : BackgroundService
{
    static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
    static readonly TimeSpan StatusRepeat = TimeSpan.FromSeconds(15);

    static readonly TimeSpan ClockPush = TimeSpan.FromMilliseconds(500);

    readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _sent = new();
    readonly object _clockGate = new();
    DateTime _clockPushedAt = DateTime.MinValue;
    DateTimeOffset _statusSentAt = DateTimeOffset.MinValue;
    string _spectatorsSent = "";
    IReadOnlyList<VmixInput> _inputs = [];

    public void ReportMusic(MusicItem music) =>
        _ = cloud.ReportAsync("sound", new { music = music.Id, label = music.Display }, CancellationToken.None);

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
        ["decks"] = string.Join(", ", deck.Decks),
        ["vmixFault"] = vmix.Fault,
        ["clockListening"] = clock.Listening,
        ["clockFault"] = clock.Fault,
        ["ledFault"] = led.Fault,
        ["cloudFault"] = cloud.Fault,
        ["states"] = states.All,
    };

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
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

        await SendIfChangedAsync("deck", new { decks = deck.Decks }, stop);

        await SendSpectatorsAsync(stop);

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

    async Task SendSpectatorsAsync(CancellationToken stop)
    {
        var game = payload.Game;
        if (game.Id.Length == 0) return;
        var mark = $"{game.Id}:{game.Spectators}";
        if (mark == _spectatorsSent) return;
        _spectatorsSent = mark;
        await cloud.SendAsync("Spectators", stop, game.Spectators);
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
