using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent;

public sealed class BreakMusic(PayloadStore payload, LiveGame live, ShowStates states, Deck.Deck deck, ILogger<BreakMusic> log) : BackgroundService
{
    static readonly TimeSpan Look = TimeSpan.FromMilliseconds(400);

    bool _playing;

    public static bool Qualifies(ClockState clock, bool clockLive, bool clockRunning)
    {
        if (!clockLive || clockRunning) return false;
        if (!clock.IsGameTime) return false;
        if (clock.Period.Length == 0) return false;
        return !AtZero(clock.Time);
    }

    static bool AtZero(string time) => !time.Any(c => c is > '0' and <= '9');

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

    void Look1()
    {
        var wanted = states.Value(ShowStates.AutoBreakMusic) == "an"
                     && payload.Hall.Offers.Sound
                     && Qualifies(live.Clock, live.ClockLive, live.ClockRunning);
        if (wanted == _playing) return;
        _playing = wanted;

        if (!wanted)
        {
            log.LogInformation("Automatik: Pausenmusik aus, {Reason}",
                live.ClockRunning ? "die Uhr läuft wieder" : "die Automatik ist ausgeschaltet");
            deck.NoteSound(null);
            return;
        }

        if (payload.Sound(payload.Hall.BreakSound) is not { } sound)
        {
            log.LogWarning("Automatik: kein Musik-Befehl für den Spielunterbruch eingestellt");
            _playing = false;
            return;
        }
        log.LogInformation("Automatik: Pausenmusik {Label} bei {Time}", sound.Label, live.Time);
        _ = deck.Play(sound);
    }
}
