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

    public string Shown { get; private set; } = "";

    public async Task RunAsync(IReadOnlyList<KeyStep> steps, KeyContext ctx, CancellationToken stop)
    {
        foreach (var step in steps)
        {
            if (stop.IsCancellationRequested) return;
            try { await StepAsync(step, ctx, stop); }
            catch (OperationCanceledException) { return; }
            catch (Exception e)
            {
                log.LogWarning("Befehl {Action} gescheitert: {Message}", step.Action, e.Message);
            }
        }
    }

    public async Task HideAllAsync(CancellationToken stop)
    {
        if (vmix.Configured) await vmix.HideAllAsync(stop);
        Shown = "";
        states.Set(ShowStates.Overlay, "aus", "ausgeblendet");
        states.Set(ShowStates.ClockVisible, "aus", "ausgeblendet");
        Changed?.Invoke();
    }

    public async Task ShowTitleAsync(string kind, KeyContext ctx, CancellationToken stop)
    {
        if (payload.Hall.Title(kind) is not { } title)
        {
            log.LogWarning("Kein Einblender für {Kind} eingestellt", kind);
            return;
        }
        if (!Offers(title)) return;
        var values = title.Fields.ToDictionary(
            field => field.Key,
            field => Templates.Expand(field.Value, ctx, payload.Game, live),
            StringComparer.OrdinalIgnoreCase);
        await vmix.ShowTitleAsync(title, values, stop);
        Shown = kind;
        states.Set(Scoreboard(kind) ? ShowStates.ClockVisible : ShowStates.Overlay, "an", kind);
        Changed?.Invoke();
    }

    public async Task HideTitleAsync(string kind, CancellationToken stop)
    {
        if (payload.Hall.Title(kind) is not { } title || !Offers(title)) return;
        await vmix.HideTitleAsync(title, stop);
        if (Shown == kind) Shown = "";
        states.Set(Scoreboard(kind) ? ShowStates.ClockVisible : ShowStates.Overlay, "aus", kind);
        Changed?.Invoke();
    }

    public async Task LedAsync(string playlist, CancellationToken stop)
    {
        if (!payload.Hall.Offers.Led) return;
        var name = playlist.StartsWith(LedPrefix, StringComparison.OrdinalIgnoreCase) ? playlist[LedPrefix.Length..] : playlist;
        if (payload.Hall.LedCommand(name) is not { Length: > 0 } command)
        {
            log.LogWarning("Keine LED-Vorlage für {Event}", name);
            return;
        }
        await led.SendAsync(command, stop);
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

    public void PlayMusic(string? musicId)
    {
        if (!payload.Hall.Offers.Sound) return;
        if (payload.MusicOf(musicId) is not { } music)
        {
            log.LogWarning("Musik-Befehl {Id} ist nicht eingestellt", musicId);
            return;
        }
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

    private async Task StepAsync(KeyStep step, KeyContext ctx, CancellationToken stop)
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
            case KeyActions.VmixShow:
                await ShowTitleAsync(KindOf(step.Target), ctx with { Event = step.Event ?? ctx.Event }, stop);
                break;
            case KeyActions.VmixHide:
                if (step.Target is null or "" or TitlePrefix + "*") await HideAllAsync(stop);
                else await HideTitleAsync(KindOf(step.Target), stop);
                break;
            case KeyActions.LedStart:
                await LedAsync(step.Target ?? "", stop);
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
    }

    private static string KindOf(string? target) =>
        target is { Length: > 0 } text && text.StartsWith(TitlePrefix, StringComparison.OrdinalIgnoreCase)
            ? text[TitlePrefix.Length..]
            : target ?? "";
}
