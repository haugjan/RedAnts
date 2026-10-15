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
        new(ClockVisible, "Scoreboard", ["aus", "an"]),
        new(ClockRunning, "Uhr", ["steht", "läuft"]),
        new(Music, "Musik", ["aus", "läuft", "fade"]),
        new(Led, "LED", ["aus", "läuft"]),
        new(Sponsors, "Sponsoren-Loop", ["aus", "an"]),
        new(Break, "Pause-Modus", ["aus", "an"]),
        new(AutoBreakMusic, "Automatik Pausenmusik", ["aus", "an"]),
        new(Light, "Licht", ["normal", "tor", "pause"]),
    ];

    public static ShowState? State(string? id) =>
        id is { Length: > 0 } ? Known.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) : null;


    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public event Action? Changed;


    public IReadOnlyDictionary<string, string> All
    {
        get { lock (_gate) return new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase); }
    }

    public string FirstValue(string stateId) => State(stateId)?.First ?? "";

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
    }


    private static Dictionary<string, string> One(string state, string value) =>
        new(StringComparer.OrdinalIgnoreCase) { [state] = value };
}
