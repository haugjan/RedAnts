namespace RedAnts.GameClock;

public sealed record ClockState(
    string Time, string HomeScore, string GuestScore, string Period,
    string Home, string Guest, string Mode, string[] Fields, string Raw, DateTime Received)
{
    public const string GameTime = "GAME TIME";
    public const string Intermission = "INTERMISSION";
    public const string TimeOut = "TIME-OUT";

    public static readonly ClockState Empty = new("--:--", "-", "-", "", "", "", "", [], "", DateTime.MinValue);

    public bool HasData => Received != DateTime.MinValue;

    public bool IsBreak => Mode.Equals(Intermission, StringComparison.OrdinalIgnoreCase)
        || Mode.Equals(TimeOut, StringComparison.OrdinalIgnoreCase);

    public string PeriodLabel => Mode.ToUpperInvariant() switch
    {
        Intermission => "PAUSE",
        TimeOut => "TIMEOUT",
        GameTime or "" => Period.Length > 0 ? $"{Period}. DRITTEL" : "",
        var other => other,
    };

    public static ClockState? Parse(string raw)
    {
        var f = raw.Split(';').Select(x => x.Trim()).ToArray();
        if (f.Length < 13) return null;
        return new ClockState(f[0], f[1], f[2], f[3], f[10], f[11], f[12], f, raw, DateTime.Now);
    }
}
