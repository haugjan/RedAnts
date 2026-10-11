using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Scenes;

public sealed class SceneRunner(PayloadStore payload, LiveGame live, ShowStates states, VmixLink vmix, SednaLink led, ILogger<SceneRunner> log)
{
    public const string PanicScene = "panic";
    const string LedPrefix = "led:";
    const string TitlePrefix = "title:";
    const int DefaultScoreTimeoutMs = 3000;

    readonly object _gate = new();
    CancellationTokenSource? _running;
    Task _current = Task.CompletedTask;
    TaskCompletionSource<(Player Player, bool OwnGoal)>? _pick;

    public string? Running { get; private set; }

    public string RunningLabel { get; private set; } = "";

    public string PickSide { get; private set; } = "";

    public bool WantsPick => _pick is not null;

    public event Action? Changed;

    public event Action<string>? ContextRequested;

    public event Action<string, object>? Reported;

    public Task Start(string sceneId, IReadOnlyDictionary<string, string> args)
    {
        var scene = payload.Scene(sceneId);
        if (scene is null)
        {
            log.LogWarning("Szene {Scene} ist nicht konfiguriert", sceneId);
            Reported?.Invoke("log", new { level = "warn", message = $"Szene {sceneId} ist nicht konfiguriert" });
            return Task.CompletedTask;
        }

        CancellationTokenSource session;
        Task previous;
        lock (_gate)
        {
            _running?.Cancel();
            _pick?.TrySetCanceled();
            _pick = null;
            session = new CancellationTokenSource();
            _running = session;
            previous = _current;
            _current = RunGuardedAsync(scene, args, previous, session);
            return _current;
        }
    }

    public void Abort()
    {
        lock (_gate)
        {
            _running?.Cancel();
            _pick?.TrySetCanceled();
            _pick = null;
        }
    }

    public bool Pick(Player player, bool ownGoal = false)
    {
        TaskCompletionSource<(Player, bool)>? pick;
        lock (_gate)
        {
            pick = _pick;
            _pick = null;
        }
        if (pick is null) return false;
        pick.TrySetResult((player, ownGoal));
        return true;
    }

    async Task RunGuardedAsync(Scene scene, IReadOnlyDictionary<string, string> args, Task previous, CancellationTokenSource session)
    {
        try { await previous; } catch (Exception) { }
        var ctx = SceneContext.From(scene, args, payload.Game, live);
        Running = scene.Id;
        RunningLabel = scene.Label;
        Changed?.Invoke();
        log.LogInformation("Szene {Scene} gestartet ({Side})", scene.Id, ctx.Side.Length > 0 ? ctx.Side : "neutral");
        try
        {
            await RunAsync(ctx, session.Token);
            log.LogInformation("Szene {Scene} beendet", scene.Id);
        }
        catch (OperationCanceledException) { log.LogInformation("Szene {Scene} abgebrochen", scene.Id); }
        catch (Exception e)
        {
            log.LogError("Szene {Scene} gescheitert: {Message}", scene.Id, e.Message);
            Reported?.Invoke("log", new { level = "error", message = $"Szene {scene.Id}: {e.Message}" });
        }
        finally
        {
            lock (_gate)
            {
                if (_running == session)
                {
                    _running = null;
                    Running = null;
                    RunningLabel = "";
                    PickSide = "";
                }
            }
            session.Dispose();
            Changed?.Invoke();
        }
    }

    async Task RunAsync(SceneContext ctx, CancellationToken stop)
    {
        foreach (var step in ctx.Scene.Steps)
        {
            stop.ThrowIfCancellationRequested();
            try
            {
                await StepAsync(step, ctx, stop);
                states.Apply(step, $"Szene {ctx.Scene.Label}");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                log.LogWarning("Schritt {Action} {Target} in {Scene}: {Message}", step.Action, step.Target, ctx.Scene.Id, e.Message);
                Reported?.Invoke("log", new { level = "warn", message = $"{ctx.Scene.Id}/{step.Action}: {e.Message}" });
            }
        }
    }

    async Task StepAsync(SceneStep step, SceneContext ctx, CancellationToken stop)
    {
        var game = payload.Game;
        switch (step.Action)
        {
            case SceneActions.SoundPlay:
            {
                var target = step.Target ?? "";
                var sound = payload.Sound(target.StartsWith(SceneActions.SoundPrefix, StringComparison.OrdinalIgnoreCase)
                    ? target[SceneActions.SoundPrefix.Length..]
                    : target);
                if (sound is null) log.LogWarning("Musik-Befehl {Target} ist nicht konfiguriert", target);
                Reported?.Invoke("sound", new { action = step.Action, target, sound = sound?.Id ?? "", label = sound?.Label ?? "", scene = ctx.Scene.Id });
                break;
            }

            case SceneActions.SoundStop:
            case SceneActions.SoundFade:
                Reported?.Invoke("sound", new { action = step.Action, target = step.Target ?? "", scene = ctx.Scene.Id, ms = step.Ms });
                break;

            case SceneActions.StateSet:
                break;

            case SceneActions.LedStart:
                await led.SendAsync(LedCommand(step, ctx, game), stop);
                break;

            case SceneActions.LedStop:
                await led.SendAsync(LedCommand(step with { Target = step.Target ?? LedPrefix + "stopp" }, ctx, game, "stop playlist"), stop);
                break;

            case SceneActions.OverlayShow:
            {
                var title = Title(step) ?? throw new InvalidOperationException($"Kein vMix-Titel {step.Target} konfiguriert");
                if (step.Event is { Length: > 0 } && ctx.Event.Length == 0) ctx.Event = step.Event;
                var values = title.Fields.ToDictionary(f => f.Key, f => Templates.Expand(f.Value, ctx, game, live));
                await vmix.ShowTitleAsync(title, values, stop);
                Reported?.Invoke("overlay", new { title = title.Kind });
                break;
            }

            case SceneActions.OverlayHide:
                if (Target(step) == "*") await vmix.HideAllAsync(stop);
                else await vmix.HideTitleAsync(Title(step) ?? throw new InvalidOperationException($"Kein vMix-Titel {step.Target} konfiguriert"), stop);
                Reported?.Invoke("overlay", new { title = vmix.LiveTitle ?? "" });
                break;

            case SceneActions.LightScene:
                log.LogDebug("Licht {Target}: noch nicht angebunden", step.Target);
                break;

            case SceneActions.DeckContext:
                ContextRequested?.Invoke(step.Target ?? "Main");
                break;

            case SceneActions.PlayerPick:
                if (ctx.Player is not null) break;
                var picked = await AwaitPickAsync(ctx.Side, stop);
                ctx.Player = picked.Player;
                if (picked.OwnGoal) ctx.Event = "owngoal";
                break;

            case SceneActions.Wait:
                await Task.Delay(step.Ms ?? 0, stop);
                break;

            case SceneActions.WaitScore:
                if (!await live.WaitForScoreChangeAsync(ctx.ScoreHomeAtStart, ctx.ScoreAwayAtStart, TimeSpan.FromMilliseconds(step.TimeoutMs ?? DefaultScoreTimeoutMs), stop))
                    log.LogWarning("Szene {Scene}: die Matchuhr meldet keinen neuen Stand, zeige trotzdem", ctx.Scene.Id);
                break;

            case SceneActions.SceneRun:
            {
                var nested = payload.Scene(step.Target ?? "") ?? throw new InvalidOperationException($"Szene {step.Target} fehlt");
                await RunAsync(ctx.Nested(nested), stop);
                break;
            }

            default:
                throw new InvalidOperationException($"Unbekannte Aktion {step.Action}");
        }
    }

    async Task<(Player Player, bool OwnGoal)> AwaitPickAsync(string side, CancellationToken stop)
    {
        var pick = new TaskCompletionSource<(Player, bool)>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            _pick = pick;
            PickSide = side;
        }
        Changed?.Invoke();
        using var registration = stop.Register(() => pick.TrySetCanceled(stop));
        try { return await pick.Task; }
        finally
        {
            lock (_gate)
            {
                if (_pick == pick) _pick = null;
                PickSide = "";
            }
            Changed?.Invoke();
        }
    }

    string LedCommand(SceneStep step, SceneContext ctx, Game game, string? fallback = null)
    {
        var name = Target(step);
        var template = payload.Hall.LedCommand(name) ?? fallback ?? throw new InvalidOperationException($"Keine LED-Vorlage {name} konfiguriert");
        var command = Templates.Expand(template, ctx, game, live);
        if (step.Overlay && !command.Contains(" -o")) command += " -o";
        if (step.FadeIn is { } fade && !command.Contains(" -fi")) command += $" -fi {fade.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return command;
    }

    OverlayTitle? Title(SceneStep step) => payload.Hall.Title(Target(step));

    static string Target(SceneStep step)
    {
        var target = step.Target ?? "";
        if (target.StartsWith(LedPrefix, StringComparison.OrdinalIgnoreCase)) return target[LedPrefix.Length..];
        if (target.StartsWith(TitlePrefix, StringComparison.OrdinalIgnoreCase)) return target[TitlePrefix.Length..];
        return target;
    }
}
