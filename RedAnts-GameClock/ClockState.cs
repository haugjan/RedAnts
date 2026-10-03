namespace RedAnts.GameClock;

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

    public static ClockState? Parse(string raw)
    {
        var f = raw.Split(';').Select(x => x.Trim()).ToArray();
        if (f.Length < 13) return null;
        return new ClockState(
            f[0], f[1], f[2], f[3], f[10], f[11], f[12],
            Penalties(f[4], f[5]), Penalties(f[6], f[7]),
            f, raw, DateTime.Now);
    }

    static IReadOnlyList<Penalty> Penalties(params string[] fields) =>
        fields.Select(Penalty.Parse).OfType<Penalty>().ToList();
}
