namespace RedAnts.GameClock.Clocks;

public sealed record Penalty(string Player, string Time)
{
    public static Penalty? Parse(string field)
    {
        var parts = field.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;
        var time = parts[^1];
        var player = string.Join(' ', parts[..^1]);
        return new Penalty(player, time);
    }
}

public sealed record ClockState(
    string Time, string HomeScore, string GuestScore, string Period,
    string Home, string Guest, string Mode,
    IReadOnlyList<Penalty> HomePenalties, IReadOnlyList<Penalty> GuestPenalties,
    string[] Fields, string Raw, DateTime Received)
{
    public const string GameTime = "GAME TIME";
    public const string Intermission = "INTERMISSION";
    public const string TimeOut = "TIME-OUT";
    public const string Overtime = "4";
    public const string Shootout = "5";

    public static readonly ClockState Empty = new("--:--", "-", "-", "", "", "", "", [], [], [], "", DateTime.MinValue);

    public bool HasData => Received != DateTime.MinValue;

    public bool IsGameTime => Mode.Length == 0 || Mode.Equals(GameTime, StringComparison.OrdinalIgnoreCase);

    public bool IsBreak => !IsGameTime;

    public bool IsLastMinute => !Time.Contains(':');

    public string PeriodLabel
    {
        get
        {
            var mode = Mode.ToUpperInvariant();
            if (mode == Intermission) return "PAUSE";
            if (mode == TimeOut) return "TIMEOUT";
            if (mode.Contains("FACE")) return "BIS SPIELBEGINN";
            if (mode.Contains("TO WARM")) return "BIS EINLAUFEN";
            if (mode.Contains("WARM")) return "EINLAUFEN";
            if (mode.Contains("LOCAL")) return "UHRZEIT";
            if (!IsGameTime) return mode;
            return Period switch
            {
                "" => "",
                Overtime => "VERLÄNGERUNG",
                Shootout => "PENALTYSCHIESSEN",
                _ => $"{Period}. DRITTEL",
            };
        }
    }

    public static IReadOnlyList<Penalty> Penalties(params string[] fields) =>
        fields.Select(Penalty.Parse).OfType<Penalty>().ToList();
}
