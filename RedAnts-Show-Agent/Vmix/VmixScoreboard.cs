using RedAnts.Show.Agent.Clock;

namespace RedAnts.Show.Agent.Vmix;

public static class VmixScoreboard
{
    public const string Kind = "scoreboard";
    public const string LuplInput = "LUPL-SU_scoreboard_clock.gtzip";
    public const string PenaltyFill = "#FFFFFF";
    const string NoPenalty = "00:00";
    const string DigitMarker = "{0}";

    static readonly string[] DigitPositions = ["M10", "M01", "T", "S10", "S01"];

    public static readonly IReadOnlyDictionary<string, string> LuplFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["time"] = "TxtClockTime{0}.Text",
        ["period"] = "TxtClockPeriod.Text",
        ["homeScore"] = "TxtHomeScore.Text",
        ["awayScore"] = "TxtAwayScore.Text",
        ["homeName"] = "TxtHomeName.Text",
        ["awayName"] = "TxtAwayName.Text",
        ["homePen1"] = "TxtHomePen1{0}.Text",
        ["homePen2"] = "TxtHomePen2{0}.Text",
        ["awayPen1"] = "TxtAwayPen1{0}.Text",
        ["awayPen2"] = "TxtAwayPen2{0}.Text",
        ["homePen1Fill"] = "RectHomePen1.Fill.Color",
        ["homePen2Fill"] = "RectHomePen2.Fill.Color",
        ["awayPen1Fill"] = "RectAwayPen1.Fill.Color",
        ["awayPen2Fill"] = "RectAwayPen2.Fill.Color",
    };

    public static IReadOnlyDictionary<string, string> Values(ClockState state, string homeName, string awayName)
    {
        var home = Penalties(state.HomePenalties);
        var away = Penalties(state.GuestPenalties);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["time"] = state.Time,
            ["period"] = state.Period switch { ClockState.Overtime => "O", ClockState.Shootout => "P", var p => p },
            ["homeScore"] = state.HomeScore,
            ["awayScore"] = state.GuestScore,
            ["homeName"] = homeName,
            ["awayName"] = awayName,
            ["homePen1"] = home[0],
            ["homePen2"] = home[1],
            ["awayPen1"] = away[0],
            ["awayPen2"] = away[1],
            ["homePen1Fill"] = Fill(home[0]),
            ["homePen2Fill"] = Fill(home[1]),
            ["awayPen1Fill"] = Fill(away[0]),
            ["awayPen2Fill"] = Fill(away[1]),
        };
    }

    public static IEnumerable<(string Field, string Value)> Expand(string field, string value)
    {
        if (!field.Contains(DigitMarker))
        {
            yield return (field, value);
            yield break;
        }

        var padded = value.Length == 0 ? "     " : value.PadRight(DigitPositions.Length);
        for (var i = 0; i < DigitPositions.Length; i++)
            yield return (field.Replace(DigitMarker, DigitPositions[i]), padded[i] is ' ' ? "" : padded[i].ToString());
    }

    public static string FunctionFor(string field) =>
        field.EndsWith(".Source", StringComparison.OrdinalIgnoreCase) ? "SetImage"
        : field.EndsWith(".Color", StringComparison.OrdinalIgnoreCase) ? "SetColor"
        : "SetText";

    static string[] Penalties(IReadOnlyList<Penalty> penalties)
    {
        var times = new string[2];
        for (var slot = 0; slot < times.Length; slot++)
        {
            var time = slot < penalties.Count ? penalties[slot].Time : "";
            times[slot] = time == NoPenalty ? "" : time;
        }
        return times;
    }

    static string Fill(string penalty) => $"{PenaltyFill}{(penalty.Length > 0 ? "FF" : "00")}";
}
