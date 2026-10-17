using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent;

public sealed class BreakMusic(PayloadStore payload, LiveGame live, ShowStates states, Deck.Deck deck, Keys.KeyRunner runner, ILogger<BreakMusic> log) : BackgroundService
{
    static readonly TimeSpan Look = TimeSpan.FromMilliseconds(150);
    static readonly TimeSpan AfterAKey = TimeSpan.FromSeconds(12);

    bool _playing;

    public static bool Qualifies(ClockState clock, bool clockLive, bool clockRunning, int periodMinutes)
    {
        if (!clockLive || clockRunning) return false;
        if (!clock.IsGameTime) return false;
        if (clock.Period.Length == 0) return false;
        return !AtZero(clock.Time) && !AtPeriodEnd(clock.Time, periodMinutes);
    }

    static bool AtZero(string time) => !time.Any(c => c is > '0' and <= '9');

    static bool AtPeriodEnd(string time, int periodMinutes) =>
        periodMinutes > 0 && time == $"{periodMinutes:00}:00";

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try { Look1(); }
            catch (Exception e) { log.LogDebug("Automatik: {Message}", e.Message); }
            try { await Task.Delay(Look, stop); }
            catch (OperationCanceledException) { break; }
        }
    }

    public static bool HeldBack(string playing, string breakMusicId, DateTime keyMusicAt, TimeSpan grace) =>
        playing.Length > 0 && !playing.Equals(breakMusicId, StringComparison.OrdinalIgnoreCase)
        || DateTime.Now - keyMusicAt < grace;

    bool Busy => HeldBack(deck.MusicId, payload.Hall.BreakMusicId, runner.KeyMusicAt, AfterAKey);

    void Look1()
    {
        var busy = Busy;
        var wanted = states.Value(ShowStates.AutoBreakMusic) == "an"
                     && payload.Hall.Offers.Sound
                     && !busy
                     && Qualifies(live.Clock, live.ClockLive, live.ClockRunning, payload.Hall.Devices.PeriodMinutes);
        if (wanted == _playing) return;
        _playing = wanted;

        if (!wanted)
        {
            if (busy)
            {
                log.LogInformation("Automatik: hält sich zurück, es läuft Musik von einer Taste");
                return;
            }
            log.LogInformation("Automatik: Pausenmusik blendet aus, {Reason}",
                live.ClockRunning ? "die Uhr läuft wieder" : "die Automatik ist ausgeschaltet");
            runner.Control("fade");
            return;
        }

        if (payload.MusicOf(payload.Hall.BreakMusicId) is not { } music)
        {
            log.LogWarning("Automatik: kein Musik-Befehl für den Spielunterbruch eingestellt");
            _playing = false;
            return;
        }
        log.LogInformation("Automatik: Pausenmusik {Label} bei {Time}", music.Display, live.Time);
        runner.PlayMusic(music.Id, automatic: true);
    }
}
