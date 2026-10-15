namespace RedAnts.Show.Domain;

public static class StateSources
{
    public const string Observed = "beobachtet";
    public const string Set = "gesetzt";

    public static readonly IReadOnlyList<string> All = [Observed, Set];
}

public sealed record ShowState(string Id, string Name, IReadOnlyList<string> Values, string Source, string Note = "")
{
    public string First => Values.Count > 0 ? Values[0] : "";

    public bool IsOn(string? value) =>
        value is { Length: > 0 } && !string.Equals(value, First, StringComparison.OrdinalIgnoreCase);
}

public static class DefaultStates
{
    public const string Overlay = "einblender";
    public const string ClockVisible = "uhr-sichtbar";
    public const string ClockRunning = "uhr-laeuft";
    public const string Music = "musik";
    public const string Led = "led";
    public const string Sponsors = "sponsoren";
    public const string AutoBreakMusic = "auto-musik";

    public static IReadOnlyList<ShowState> All { get; } =
    [
        new(Overlay, "Einblender", ["aus", "an"], StateSources.Observed, "aktive Overlays in vMix"),
        new(ClockVisible, "Scoreboard", ["aus", "an"], StateSources.Observed, "Scoreboard-Overlay in vMix"),
        new(ClockRunning, "Uhr", ["steht", "läuft"], StateSources.Observed, "Zeit im Telegramm bewegt sich"),
        new(Music, "Musik", ["aus", "läuft", "pause"], StateSources.Observed, "Wiedergabe auf dem Bord"),
        new(Led, "LED", ["aus", "läuft"], StateSources.Set, "sedna meldet nichts zurück"),
        new(Sponsors, "Sponsoren-Loop", ["aus", "an"], StateSources.Set),
        new(AutoBreakMusic, "Auto-Pausenmusik", ["aus", "an"], StateSources.Set, "spielt die Pausenmusik, sobald die Uhr mitten im Drittel steht"),
    ];

    public static ShowState? Of(string id) => All.FirstOrDefault(s => s.Id == id);

    public static string? StateOf(string special) => special switch
    {
        SpecialKey.OverlayToggle => Overlay,
        SpecialKey.ClockToggle => ClockVisible,
        SpecialKey.MusicToggle => Music,
        SpecialKey.AutoBreakMusic => AutoBreakMusic,
        _ => null,
    };

    public static IReadOnlyDictionary<string, string> Effects(KeyStep step)
    {
        var target = step.Target ?? "";
        var scoreboard = target.Equals("title:scoreboard", StringComparison.OrdinalIgnoreCase);
        return step.Action switch
        {
            KeyActions.VmixShow => One(scoreboard ? ClockVisible : Overlay, "an"),
            KeyActions.VmixHide => One(scoreboard ? ClockVisible : Overlay, "aus"),
            KeyActions.MusicPlay => One(Music, "läuft"),
            KeyActions.MusicStop => One(Music, "aus"),
            KeyActions.LedStart => target.EndsWith(":sponsoren", StringComparison.OrdinalIgnoreCase)
                ? new Dictionary<string, string> { [Led] = "läuft", [Sponsors] = "an" }
                : One(Led, "läuft"),
            KeyActions.LedStop => new Dictionary<string, string> { [Led] = "aus", [Sponsors] = "aus" },
            _ => new Dictionary<string, string>(),
        };
    }

    private static Dictionary<string, string> One(string state, string value) => new() { [state] = value };
}
