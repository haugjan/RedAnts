using RedAnts.GameClock.Clocks;

namespace RedAnts.GameClock.Vmix;

public sealed record VmixScene(string HomeName, string AwayName, string HomeLogo, string AwayLogo, string PenaltyFill);

public sealed record VmixPayload(
    IReadOnlyDictionary<string, string> Text,
    IReadOnlyDictionary<string, string> Colors,
    IReadOnlyDictionary<string, string> Images)
{
    const string NoPenalty = "00:00";

    public static VmixPayload Build(ClockState state, VmixScene scene)
    {
        var home = Penalties(state.HomePenalties);
        var away = Penalties(state.GuestPenalties);

        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [VmixSlots.Time] = state.Time,
            [VmixSlots.Period] = PeriodValue(state),
            [VmixSlots.HomeScore] = state.HomeScore,
            [VmixSlots.AwayScore] = state.GuestScore,
            [VmixSlots.HomeName] = scene.HomeName,
            [VmixSlots.AwayName] = scene.AwayName,
            [VmixSlots.HomePenalty1] = home[0],
            [VmixSlots.HomePenalty2] = home[1],
            [VmixSlots.AwayPenalty1] = away[0],
            [VmixSlots.AwayPenalty2] = away[1],
        };

        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [VmixSlots.HomePenalty1Fill] = Fill(scene.PenaltyFill, home[0]),
            [VmixSlots.HomePenalty2Fill] = Fill(scene.PenaltyFill, home[1]),
            [VmixSlots.AwayPenalty1Fill] = Fill(scene.PenaltyFill, away[0]),
            [VmixSlots.AwayPenalty2Fill] = Fill(scene.PenaltyFill, away[1]),
        };

        var images = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [VmixSlots.HomeLogo] = scene.HomeLogo,
            [VmixSlots.AwayLogo] = scene.AwayLogo,
        };

        return new VmixPayload(text, colors, images);
    }

    static string PeriodValue(ClockState state) => state.Period switch
    {
        ClockState.Overtime => "O",
        ClockState.Shootout => "P",
        var period => period,
    };

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

    static string Fill(string color, string penalty) => $"{color}{(penalty.Length > 0 ? "FF" : "00")}";
}
