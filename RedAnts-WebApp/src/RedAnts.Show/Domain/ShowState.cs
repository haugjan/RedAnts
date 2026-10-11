namespace RedAnts.Show.Domain;

public static class StateSources
{
    public const string Observed = "beobachtet";
    public const string Set = "gesetzt";
    public const string Logical = "logisch";

    public static readonly IReadOnlyList<string> All = [Observed, Set, Logical];
}

public sealed record ShowState(string Id, string Name, IReadOnlyList<string> Values, string Source, string Note = "")
{
    public string First => Values.Count > 0 ? Values[0] : "";

    public bool IsOn(string? value) =>
        value is { Length: > 0 } && !string.Equals(value, First, StringComparison.OrdinalIgnoreCase);

    public ShowState Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Name");
        if (Values.Count < 2) throw new ValidationException(nameof(Values), $"Zustand „{Name}“ braucht mindestens zwei Werte.");
        if (Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != Values.Count)
            throw new ValidationException(nameof(Values), $"Zustand „{Name}“ hat doppelte Werte.");
        return this with { Id = Id.Trim(), Name = Name.Trim(), Values = Values.Select(v => v.Trim()).ToList() };
    }
}

public sealed record AutomationRule(string StateId, string Value, string SceneId);

public static class DefaultStates
{
    public const string Overlay = "einblender";
    public const string ClockVisible = "uhr-sichtbar";
    public const string ClockRunning = "uhr-laeuft";
    public const string Music = "musik";
    public const string Led = "led";
    public const string Sponsors = "sponsoren";
    public const string Break = "pause";
    public const string Light = "licht";

    public static IReadOnlyList<ShowState> All { get; } =
    [
        new(Overlay, "Einblender", ["aus", "an"], StateSources.Observed, "aktive Overlays in vMix"),
        new(ClockVisible, "Uhr sichtbar", ["aus", "an"], StateSources.Observed, "Scoreboard-Overlay in vMix"),
        new(ClockRunning, "Uhr läuft", ["steht", "läuft"], StateSources.Observed, "Zeit im Telegramm bewegt sich"),
        new(Music, "Musik", ["aus", "läuft", "fade"], StateSources.Observed, "Bedienung im Browser"),
        new(Led, "LED", ["aus", "läuft"], StateSources.Set, "sedna meldet nichts zurück"),
        new(Sponsors, "Sponsoren-Loop", ["aus", "an"], StateSources.Set),
        new(Break, "Pause-Modus", ["aus", "an"], StateSources.Logical),
        new(Light, "Licht", ["normal", "tor", "pause"], StateSources.Set, "Moving Heads kommen später"),
    ];

    public static IReadOnlyDictionary<string, string> Effects(SceneStep step)
    {
        var target = step.Target ?? "";
        var scoreboard = target.Equals("title:scoreboard", StringComparison.OrdinalIgnoreCase);
        return step.Action switch
        {
            SceneActions.OverlayShow => One(scoreboard ? ClockVisible : Overlay, "an"),
            SceneActions.OverlayHide => One(scoreboard ? ClockVisible : Overlay, "aus"),
            SceneActions.SoundPlay => One(Music, "läuft"),
            SceneActions.SoundStop => One(Music, "aus"),
            SceneActions.SoundFade => One(Music, "fade"),
            SceneActions.LedStart => target.EndsWith(":sponsoren", StringComparison.OrdinalIgnoreCase)
                ? new Dictionary<string, string> { [Led] = "läuft", [Sponsors] = "an" }
                : One(Led, "läuft"),
            SceneActions.LedStop => new Dictionary<string, string> { [Led] = "aus", [Sponsors] = "aus" },
            SceneActions.LightScene => One(Light, target.Replace("light:", "", StringComparison.OrdinalIgnoreCase)),
            SceneActions.StateSet when step.Event is { Length: > 0 } => One(target, step.Event),
            _ => new Dictionary<string, string>(),
        };
    }

    private static Dictionary<string, string> One(string state, string value) => new() { [state] = value };
}
