using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Keys;

public sealed class KeyRunner(
    PayloadStore payload,
    LiveGame live,
    ShowStates states,
    VmixLink vmix,
    SednaLink led,
    ILogger<KeyRunner> log)
{
    public const string TitlePrefix = "title:";
    public const string LedPrefix = "led:";

    public event Action<MusicItem>? MusicRequested;
    public event Action? MusicStopped;
    public event Action<string>? MusicControl;
    public event Action? Changed;
    public event Action<string>? Failed;

    public string Shown { get; private set; } = "";

    long _tokens;
    long _shown;

    public string OnAir { get; private set; } = "";

    public string ShownSide { get; private set; } = "";

    public async Task RunAsync(IReadOnlyList<KeyStep> steps, KeyContext ctx, CancellationToken stop)
    {
        var shown = 0L;
        foreach (var step in steps)
        {
            if (stop.IsCancellationRequested) return;
            try { shown = await StepAsync(step, ctx, shown, stop); }
            catch (OperationCanceledException) { return; }
            catch (Exception e)
            {
                log.LogWarning("Befehl {Action} gescheitert: {Message}", step.Action, e.Message);
                Failed?.Invoke(e.Message);
            }
        }
    }

    public async Task HideAllAsync(CancellationToken stop)
    {
        if (vmix.Configured) await vmix.HideAllAsync(stop);
        Shown = "";
        ShownSide = "";
        OnAir = "";
        states.Set(ShowStates.Overlay, "aus", "ausgeblendet");
        Changed?.Invoke();
    }

    public async Task<long> ShowTitleAsync(string kind, KeyContext ctx, CancellationToken stop)
    {
        if (payload.Hall.Title(kind) is not { } title)
        {
            log.LogWarning("Kein Einblender für {Kind} eingestellt", kind);
            return 0;
        }
        if (!Offers(title)) return 0;
        var values = title.Fields.ToDictionary(
            field => field.Key,
            field => Templates.Expand(field.Value, ctx, payload.Game, live),
            StringComparer.OrdinalIgnoreCase);
        await vmix.ShowTitleAsync(title, values, stop);
        Shown = kind;
        ShownSide = ctx.Side;
        OnAir = OnAirText(kind, ctx);
        _shown = Interlocked.Increment(ref _tokens);
        states.Set(Scoreboard(kind) ? ShowStates.ClockVisible : ShowStates.Overlay, "an", kind);
        Changed?.Invoke();
        return _shown;
    }

    public Task HideTitleAsync(string kind, CancellationToken stop) => HideTitleAsync(kind, 0, stop);

    public async Task HideTitleAsync(string kind, long shown, CancellationToken stop)
    {
        if (payload.Hall.Title(kind) is not { } title || !Offers(title)) return;
        if (shown != 0 && shown != _shown) return;
        await vmix.HideTitleAsync(title, stop);
        if (Shown == kind) { Shown = ""; ShownSide = ""; OnAir = ""; }
        states.Set(Scoreboard(kind) ? ShowStates.ClockVisible : ShowStates.Overlay, "aus", kind);
        Changed?.Invoke();
    }

    public const string PlayerPlaylist = "spielerin";
    public const string NumberPlaylist = "tor";
    public const string AssistPlaylist = "assist";
    public const string GoalEvent = "goal";

    public async Task LedAsync(string playlist, KeyContext ctx, CancellationToken stop)
    {
        if (!payload.Hall.Offers.Led) return;
        var name = playlist.StartsWith(LedPrefix, StringComparison.OrdinalIgnoreCase) ? playlist[LedPrefix.Length..] : playlist;
        if (payload.Hall.LedCommand(name) is not { Length: > 0 } command)
        {
            log.LogWarning("Keine LED-Vorlage für {Event}", name);
            return;
        }
        await led.SendAsync(Templates.Expand(command, ctx, payload.Game, live), stop);
        states.Set(ShowStates.Led, "läuft", name);
        if (name.Equals("sponsoren", StringComparison.OrdinalIgnoreCase)) states.Set(ShowStates.Sponsors, "an", name);
        Changed?.Invoke();
    }

    public async Task LedStopAsync(CancellationToken stop)
    {
        if (!payload.Hall.Offers.Led) return;
        if (payload.Hall.LedCommand("stopp") is { Length: > 0 } command) await led.SendAsync(command, stop);
        states.Set(ShowStates.Led, "aus", "gestoppt");
        states.Set(ShowStates.Sponsors, "aus", "gestoppt");
        Changed?.Invoke();
    }

    public Task ShowPlayerOnLedAsync(KeyContext ctx, CancellationToken stop) =>
        PlayerLedAsync(PlayerLed(ctx.Event), ctx, stop);

    public Task ShowAssistOnLedAsync(KeyContext ctx, CancellationToken stop) =>
        PlayerLedAsync(payload.Hall.LedCommand(AssistPlaylist) is not null ? AssistPlaylist : PlayerLed(GoalEvent), ctx, stop);

    public async Task ShowGuestLogoOnLedAsync(KeyContext ctx, CancellationToken stop)
    {
        if (!payload.Hall.Offers.Led) return;
        var team = payload.Game.Side(ctx.Side);
        if (GuestTeams.EventOf(team) is not { } playlist)
        {
            log.LogWarning("Keine LED-Vorlage für den Gast {Team}", team.LongName);
            return;
        }
        try { await LedAsync(playlist, ctx, stop); }
        catch (Exception e)
        {
            log.LogWarning("Das Gastlogo ging nicht an die LED-Wand: {Message}", e.Message);
            Failed?.Invoke(e.Message);
        }
    }

    string PlayerLed(string occasion) =>
        payload.Hall.LedCommand(PlayerPlaylist) is not null ? PlayerPlaylist
        : occasion.Equals(GoalEvent, StringComparison.OrdinalIgnoreCase) ? NumberPlaylist
        : "";

    async Task PlayerLedAsync(string playlist, KeyContext ctx, CancellationToken stop)
    {
        if (!payload.Hall.Offers.Led || ctx.Player is null || playlist.Length == 0) return;
        if (!ctx.Side.Equals(payload.Game.OwnSide(payload.Hall.TeamId), StringComparison.OrdinalIgnoreCase)) return;
        try { await LedAsync(playlist, ctx, stop); }
        catch (Exception e)
        {
            log.LogWarning("Spielerin ging nicht an die LED-Wand: {Message}", e.Message);
            Failed?.Invoke(e.Message);
        }
    }

    public DateTime KeyMusicAt { get; private set; } = DateTime.MinValue;

    public void PlayMusic(string? musicId) => PlayMusic(musicId, false);

    public void PlayMusic(string? musicId, bool automatic)
    {
        if (!payload.Hall.Offers.Sound) return;
        if (payload.MusicOf(musicId) is not { } music)
        {
            log.LogWarning("Musik-Befehl {Id} ist nicht eingestellt", musicId);
            return;
        }
        states.Set(ShowStates.Music, "läuft", music.Name);
        if (!automatic) KeyMusicAt = DateTime.Now;
        MusicRequested?.Invoke(music);
    }

    public void StopMusic()
    {
        MusicStopped?.Invoke();
        states.Set(ShowStates.Music, "aus", "gestoppt");
        Changed?.Invoke();
    }

    public void Control(string what)
    {
        MusicControl?.Invoke(what);
        if (what == "fade") states.Set(ShowStates.Music, "aus", "ausgeblendet");
        Changed?.Invoke();
    }

    private bool Offers(OverlayTitle title) =>
        payload.Hall.Offers.Vmix && (vmix.Configured || Warn(title));

    private bool Warn(OverlayTitle title)
    {
        log.LogWarning("vMix ist für diese Halle nicht eingerichtet, {Kind} bleibt aus", title.Kind);
        return false;
    }

    private static bool Scoreboard(string kind) => kind.Equals("scoreboard", StringComparison.OrdinalIgnoreCase);

    private async Task<long> StepAsync(KeyStep step, KeyContext ctx, long shown, CancellationToken stop)
    {
        switch (step.Action)
        {
            case KeyActions.MusicPlay:
                PlayMusic(step.Target);
                break;
            case KeyActions.MusicStop:
                StopMusic();
                break;
            case KeyActions.MusicFade:
                Control("fade");
                break;
            case KeyActions.VmixText:
                shown = await ShowTitleAsync(step.Target is { Length: > 0 } named ? KindOf(named) : "message", ctx with
                {
                    Text = step.Text.Length > 0 ? step.Text : ctx.Text,
                }, stop);
                break;
            case KeyActions.VmixShow:
                shown = await ShowTitleAsync(KindOf(step.Target), ctx with
                {
                    Side = step.Side.Length > 0 ? step.Side : ctx.Side,
                    Event = step.Event ?? ctx.Event,
                    Text = step.Text.Length > 0 ? step.Text : ctx.Text,
                }, stop);
                break;
            case KeyActions.VmixHide:
                if (step.Target is null or "" or TitlePrefix + "*") await HideAllAsync(stop);
                else await HideTitleAsync(KindOf(step.Target), shown, stop);
                break;
            case KeyActions.LedStart:
                await LedAsync(step.Target ?? "", ctx, stop);
                break;
            case KeyActions.LedStop:
                await LedStopAsync(stop);
                break;
            case KeyActions.Wait:
                await Task.Delay(Math.Clamp(step.Ms ?? 0, 0, 120000), stop);
                break;
            default:
                log.LogWarning("Unbekannter Befehl {Action}", step.Action);
                break;
        }
        return shown;
    }

    private static string OnAirText(string kind, KeyContext ctx)
    {
        var name = NameOf(kind, ctx.Event);
        return ctx.Player is { } player ? name + "\n" + player.Display + " " + player.Name : name;
    }

    private static string NameOf(string kind, string occasion) => kind switch
    {
        "player_event" => occasion.Equals("owngoal", StringComparison.OrdinalIgnoreCase) ? "Eigentor" : "Tor",
        "penalty" => "Strafe",
        "bestplayer" => "Best Player",
        "name" => "Spielerin",
        "starting6" => "Starting 6",
        "coach" => "Coach",
        "timeout" => "Timeout",
        "message" => "Meldung",
        "commentary" => "Kommentar",
        "referee" => "Schiedsrichter",
        "result" => "Resultat",
        "opener" => "Opener",
        "scoreboard" => "Scoreboard",
        "lineup_home" or "lineup_away" => "Aufstellung",
        _ => kind,
    };

    private static string KindOf(string? target) =>
        target is { Length: > 0 } text && text.StartsWith(TitlePrefix, StringComparison.OrdinalIgnoreCase)
            ? text[TitlePrefix.Length..]
            : target ?? "";
}
