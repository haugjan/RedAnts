using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Cloud;

public sealed class StatusReporter(
    CloudLink cloud,
    PayloadStore payload,
    LiveGame live,
    ClockFeed clock,
    VmixLink vmix,
    SednaLink led,
    SceneRunner runner,
    Deck.Deck deck) : BackgroundService
{
    static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
    static readonly TimeSpan StatusRepeat = TimeSpan.FromSeconds(15);

    readonly Dictionary<string, string> _sent = [];
    DateTimeOffset _statusSentAt = DateTimeOffset.MinValue;

    public Dictionary<string, object?> Status() => new()
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
        ["clockReceived"] = clock.Received,
        ["ledFault"] = led.Fault,
        ["cloudFault"] = cloud.Fault,
    };

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        runner.Reported += Forward;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                await SendAsync(stop);
                try { await Task.Delay(Tick, stop); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally { runner.Reported -= Forward; }
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

        var state = live.Clock;
        var clockBody = new { time = live.Time, period = live.Period, periodLabel = live.PeriodLabel, mode = state.Mode, live = live.ClockLive };
        await SendIfChangedAsync("clock", clockBody, stop);

        await SendIfChangedAsync("deck", new { context = deck.Context.ToString(), scene = runner.Running ?? "" }, stop);

        var status = Status();
        var json = System.Text.Json.JsonSerializer.Serialize(status, PayloadJson.Options);
        var due = DateTimeOffset.Now - _statusSentAt > StatusRepeat;
        if (due || !_sent.TryGetValue("status", out var previous) || previous != json)
        {
            _sent["status"] = json;
            _statusSentAt = DateTimeOffset.Now;
            await cloud.ReportAsync("status", status, stop);
        }
    }

    async Task SendIfChangedAsync(string kind, object body, CancellationToken stop)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(body, PayloadJson.Options);
        if (_sent.TryGetValue(kind, out var previous) && previous == json) return;
        _sent[kind] = json;
        await cloud.ReportAsync(kind, body, stop);
    }
}
