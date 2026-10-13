using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent;

public sealed class ShowStates(PayloadStore payload, ILogger<ShowStates> log)
{
    public const string Overlay = "einblender";
    public const string ClockVisible = "uhr-sichtbar";
    public const string ClockRunning = "uhr-laeuft";
    public const string Music = "musik";
    public const string Led = "led";
    public const string Sponsors = "sponsoren";
    public const string Break = "pause";
    public const string AutoBreakMusic = "auto-musik";
    public const string Light = "licht";

    public sealed record ShowState(string Id, string Name, IReadOnlyList<string> Values)
    {
        public string First => Values.Count > 0 ? Values[0] : "";
    }

    public static IReadOnlyList<ShowState> Known { get; } =
    [
        new(Overlay, "Einblender", ["aus", "an"]),
        new(ClockVisible, "Uhr sichtbar", ["aus", "an"]),
        new(ClockRunning, "Uhr läuft", ["steht", "läuft"]),
        new(Music, "Musik", ["aus", "läuft", "fade"]),
        new(Led, "LED", ["aus", "läuft"]),
        new(Sponsors, "Sponsoren-Loop", ["aus", "an"]),
        new(Break, "Pause-Modus", ["aus", "an"]),
        new(AutoBreakMusic, "Automatik Pausenmusik", ["aus", "an"]),
        new(Light, "Licht", ["normal", "tor", "pause"]),
    ];

    public static ShowState? State(string? id) =>
        id is { Length: > 0 } ? Known.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) : null;

    private const int MaxChain = 3;

    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private int _depth;

    public event Action? Changed;

    public event Action<string, string>? RuleTriggered;

    public IReadOnlyDictionary<string, string> All
    {
        get { lock (_gate) return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase); }
    }

    public string Value(string stateId)
    {
        lock (_gate)
            if (_values.TryGetValue(stateId, out var value)) return value;
        return State(stateId)?.First ?? "";
    }

    public bool IsOn(string stateId)
    {
        var state = State(stateId);
        var value = Value(stateId);
        return state is not null && value.Length > 0 && !value.Equals(state.First, StringComparison.OrdinalIgnoreCase);
    }

    public void Set(string stateId, string value, string reason)
    {
        if (stateId.Length == 0 || value.Length == 0) return;
        lock (_gate)
        {
            if (_values.TryGetValue(stateId, out var current) && current == value) return;
            _values[stateId] = value;
        }
        log.LogInformation("Zustand {State}: {Value} ({Reason})", stateId, value, reason);
        Changed?.Invoke();
        Automate(stateId, value);
    }

    public void Apply(SceneStep step, string reason)
    {
        foreach (var (state, value) in Effects(step)) Set(state, value, reason);
    }

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
            SceneActions.StateSet when step.Event is { Length: > 0 } value => One(target, value),
            _ => new Dictionary<string, string>(),
        };
    }

    private void Automate(string stateId, string value)
    {
        if (_depth >= MaxChain) return;
        foreach (var rule in payload.Hall.Rules.Where(r => r.StateId == stateId && r.Value == value))
        {
            _depth++;
            try
            {
                log.LogInformation("Automatik: {State} wird {Value}, Szene {Scene}", stateId, value, rule.SceneId);
                RuleTriggered?.Invoke(rule.SceneId, $"Automatik {stateId}={value}");
            }
            finally { _depth--; }
        }
    }

    private static Dictionary<string, string> One(string state, string value) =>
        new(StringComparer.OrdinalIgnoreCase) { [state] = value };
}
