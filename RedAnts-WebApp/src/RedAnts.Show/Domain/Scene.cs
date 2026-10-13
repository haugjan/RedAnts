namespace RedAnts.Show.Domain;

public static class SceneActions
{
    public const string SoundPlay = "sound.play";
    public const string SoundStop = "sound.stop";
    public const string SoundFade = "sound.fade";
    public const string LedStart = "led.start";
    public const string LedStop = "led.stop";
    public const string OverlayShow = "overlay.show";
    public const string OverlayHide = "overlay.hide";
    public const string LightScene = "light.scene";
    public const string DeckContext = "deck.context";
    public const string PlayerPick = "player.pick";
    public const string Wait = "wait";
    public const string WaitScore = "wait.score";
    public const string SceneRun = "scene.run";
    public const string StateSet = "state.set";

    public static readonly IReadOnlyList<string> All =
    [
        SoundPlay, SoundStop, SoundFade, LedStart, LedStop, OverlayShow, OverlayHide,
        LightScene, DeckContext, PlayerPick, Wait, WaitScore, SceneRun, StateSet,
    ];

    public static string Label(string action) => action switch
    {
        SoundPlay => "Musik abspielen",
        SoundStop => "Musik stoppen",
        SoundFade => "Musik ausblenden",
        LedStart => "LED-Playlist starten",
        LedStop => "LED-Playlist stoppen",
        OverlayShow => "Einblender zeigen",
        OverlayHide => "Einblender ausblenden",
        LightScene => "Lichtszene (später)",
        DeckContext => "Deck-Seite wechseln",
        PlayerPick => "Auf Spielerinnenwahl warten",
        Wait => "Warten",
        WaitScore => "Auf neuen Stand der Uhr warten",
        SceneRun => "Andere Szene ausführen",
        StateSet => "Zustand setzen",
        _ => action,
    };
}

public sealed record SceneStep(
    string Action,
    string? Target = null,
    int? Ms = null,
    int? TimeoutMs = null,
    bool Overlay = false,
    double? FadeIn = null,
    string? Event = null);

public sealed record Scene(string Id, string Label, string Side, IReadOnlyList<SceneStep> Steps, bool System = false)
{
    public IReadOnlyDictionary<string, string> Effects
    {
        get
        {
            var effects = new Dictionary<string, string>();
            foreach (var step in Steps)
                foreach (var (state, value) in DefaultStates.Effects(step))
                    effects[state] = value;
            return effects;
        }
    }

    public Scene Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(Label, nameof(Label), "Bezeichnung");
        var unknown = Steps.Select(s => s.Action).FirstOrDefault(a => !SceneActions.All.Contains(a));
        if (unknown is not null) throw new ValidationException(nameof(Steps), $"Unbekannte Aktion „{unknown}“ in Szene {Id}.");
        return this;
    }
}

public static class DefaultScenes
{
    private const string None = "";

    public static IReadOnlyList<Scene> All { get; } =
    [
        Goal("tor-heim", "Tor Heim", "home", "led:tor-heim"),
        Goal("tor-gast", "Tor Gast", "away", "led:tor-gast"),
        PlayerOverlay("strafe-heim", "Strafe Heim", "home", "PlayerHome", "penalty"),
        PlayerOverlay("strafe-gast", "Strafe Gast", "away", "PlayerAway", "penalty"),
        PlayerOverlay("name-heim", "Name Heim", "home", "PlayerHome", "name"),
        PlayerOverlay("name-gast", "Name Gast", "away", "PlayerAway", "name"),
        PlayerOverlay("bestplayer-heim", "Best Player Heim", "home", "PlayerHome", "bestplayer"),
        PlayerOverlay("bestplayer-gast", "Best Player Gast", "away", "PlayerAway", "bestplayer"),
        Fixed("coach-heim", "Coach Heim", "home", "coach"),
        Fixed("coach-gast", "Coach Gast", "away", "coach"),
        Fixed("timeout-heim", "Timeout Heim", "home", "timeout", "led:timeout"),
        Fixed("timeout-gast", "Timeout Gast", "away", "timeout", "led:timeout"),
        Fixed("aufstellung-heim", "Aufstellung Heim", "home", "lineup_home"),
        Fixed("aufstellung-gast", "Aufstellung Gast", "away", "lineup_away"),
        Fixed("starting6-heim", "Starting 6 Heim", "home", "starting6"),
        Fixed("starting6-gast", "Starting 6 Gast", "away", "starting6"),
        Fixed("kommentatoren", "Kommentatoren", None, "commentary"),
        Fixed("schiedsrichter", "Schiedsrichter", None, "referee"),
        Fixed("opener", "Opener", None, "opener"),
        Fixed("resultat", "Resultat", None, "result"),
        Fixed("mitteilung", "Mitteilung", None, "message"),
        Fixed("highlight", "Highlight", None, "result"),
        new("pause", "Pause", None, [new(SceneActions.LedStart, "led:pause")]),
        new("intermission", "Intermission", None, [new(SceneActions.LedStart, "led:intermission")]),
        new("sponsoren", "Sponsoren-Loop", None, [new(SceneActions.LedStart, "led:sponsoren")]),
        new("ausblenden", "Ausblenden", None, [new(SceneActions.OverlayHide, "title:*")]),
    ];

    public static IReadOnlyList<Scene> System { get; } =
    [
        Sys("panic", "PANIC",
        [
            new(SceneActions.OverlayHide, "title:*"),
            new(SceneActions.LedStop, "led:stopp"),
            new(SceneActions.SoundStop),
            new(SceneActions.StateSet, DefaultStates.Break, Event: "aus"),
            new(SceneActions.DeckContext, "Main"),
        ]),
        Sys("sys-ausblenden", "Ausblenden (alle Einblender)", [new(SceneActions.OverlayHide, "title:*")]),
        Sys("sys-einblenden", "Letzten Einblender zeigen", [new(SceneActions.OverlayShow, "title:result", Event: "result")]),
        Sys("sys-uhr-ein", "Uhr einblenden", [new(SceneActions.OverlayShow, "title:scoreboard")]),
        Sys("sys-uhr-aus", "Uhr ausblenden", [new(SceneActions.OverlayHide, "title:scoreboard")]),
        Sys("sys-automusik-ein", "Automatik Pausenmusik ein", [new(SceneActions.StateSet, DefaultStates.AutoBreakMusic, Event: "an")]),
        Sys("sys-automusik-aus", "Automatik Pausenmusik aus", [new(SceneActions.StateSet, DefaultStates.AutoBreakMusic, Event: "aus")]),
        Sys("sys-musik-stopp", "Musik Stopp", [new(SceneActions.SoundStop)]),
        Sys("sys-musik-fade", "Musik Fade out", [new(SceneActions.SoundFade, Ms: 2000)]),
        Sys("sys-led-stopp", "LED Stopp", [new(SceneActions.LedStop, "led:stopp")]),
    ];

    public static IReadOnlyList<Scene> WithSystem(IReadOnlyList<Scene> scenes) =>
        scenes.Where(s => !s.System).Concat(System).ToList();

    private static Scene Sys(string id, string label, IReadOnlyList<SceneStep> steps) => new(id, label, None, steps, System: true);

    private static Scene Goal(string id, string label, string side, string led) => new(id, label, side,
    [
        new(SceneActions.SoundPlay, "sound:tor"),
        new(SceneActions.LedStart, led, Overlay: true, FadeIn: 0.5),
        new(SceneActions.LightScene, "light:tor"),
        new(SceneActions.DeckContext, side == "home" ? "GoalHome" : "GoalAway"),
        new(SceneActions.PlayerPick),
        new(SceneActions.WaitScore, TimeoutMs: 3000),
        new(SceneActions.OverlayShow, "title:player_event", Event: "goal"),
        new(SceneActions.LedStart, "led:spielerin", Overlay: true),
        new(SceneActions.Wait, Ms: 8000),
        new(SceneActions.OverlayHide, "title:player_event"),
        new(SceneActions.DeckContext, "Main"),
    ]);

    private static Scene PlayerOverlay(string id, string label, string side, string context, string kind) => new(id, label, side,
    [
        new(SceneActions.DeckContext, context),
        new(SceneActions.PlayerPick),
        new(SceneActions.OverlayShow, "title:" + kind, Event: kind),
        new(SceneActions.Wait, Ms: 8000),
        new(SceneActions.OverlayHide, "title:" + kind),
        new(SceneActions.DeckContext, "Main"),
    ]);

    private static Scene Fixed(string id, string label, string side, string kind, string? led = null)
    {
        var steps = new List<SceneStep>();
        if (led is not null) steps.Add(new(SceneActions.LedStart, led, Overlay: true));
        steps.Add(new(SceneActions.OverlayShow, "title:" + kind, Event: kind));
        steps.Add(new(SceneActions.Wait, Ms: 8000));
        steps.Add(new(SceneActions.OverlayHide, "title:" + kind));
        return new Scene(id, label, side, steps);
    }
}
