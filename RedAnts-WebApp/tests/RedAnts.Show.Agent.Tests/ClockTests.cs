using System.Text;
using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Clock;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class ClockTests
{
    const string Telegram = "12:34;3;1;2;7 02:00;;;;;;Red Ants;Gegner;GAME TIME";

    [Fact]
    public void Parses_an_icast_telegram_in_utf32()
    {
        var bytes = new UTF32Encoding(bigEndian: true, byteOrderMark: false).GetBytes(Telegram);

        var state = new ICastReader().Read(bytes);

        Assert.NotNull(state);
        Assert.Equal("12:34", state.Time);
        Assert.Equal("3", state.HomeScore);
        Assert.Equal("1", state.GuestScore);
        Assert.Equal("2", state.Period);
        Assert.Equal("Red Ants", state.Home);
        Assert.Equal("GAME TIME", state.Mode);
        Assert.Equal("2. DRITTEL", state.PeriodLabel);
        var penalty = Assert.Single(state.HomePenalties);
        Assert.Equal(("7", "02:00"), (penalty.Player, penalty.Time));
    }

    [Fact]
    public void Rejects_short_lines()
    {
        Assert.Null(ICastProtocol.Parse("12:34;3;1"));
    }

    [Fact]
    public void Live_game_falls_back_to_the_manual_score_without_telegrams()
    {
        var live = new LiveGame();
        Assert.Equal("0", live.HomeScore);

        live.AdjustScore("home", +1);
        live.AdjustScore("away", -1);

        Assert.Equal("1", live.HomeScore);
        Assert.Equal("0", live.AwayScore);
        Assert.False(live.ClockLive);
    }

    [Fact]
    public async Task Waits_for_the_score_to_move()
    {
        var live = new LiveGame();
        live.Update(ICastProtocol.Parse(Telegram)!);
        var wait = live.WaitForScoreChangeAsync("3", "1", TimeSpan.FromSeconds(5), CancellationToken.None);

        live.Update(ICastProtocol.Parse(Telegram.Replace("12:34;3;1", "12:30;4;1"))!);

        Assert.True(await wait);
        Assert.Equal("4", live.HomeScore);
    }

    [Fact]
    public async Task Gives_up_when_the_score_stays()
    {
        var live = new LiveGame();
        Assert.False(await live.WaitForScoreChangeAsync("0", "0", TimeSpan.FromMilliseconds(50), CancellationToken.None));
    }

    [Fact]
    public void Scoreboard_values_split_the_clock_into_digits()
    {
        var state = ICastProtocol.Parse(Telegram)!;
        var values = VmixScoreboard.Values(state, "Heim", "Gast");

        Assert.Equal("12:34", values["time"]);
        Assert.Equal("02:00", values["homePen1"]);
        Assert.Equal("#FFFFFFFF", values["homePen1Fill"]);
        Assert.Equal("#FFFFFF00", values["awayPen1Fill"]);

        var digits = VmixScoreboard.Expand("TxtClockTime{0}.Text", "12:34").ToList();
        Assert.Equal(["TxtClockTimeM10.Text", "TxtClockTimeM01.Text", "TxtClockTimeT.Text", "TxtClockTimeS10.Text", "TxtClockTimeS01.Text"], digits.Select(d => d.Field));
        Assert.Equal(["1", "2", ":", "3", "4"], digits.Select(d => d.Value));
        Assert.Equal("SetColor", VmixScoreboard.FunctionFor("RectHomePen1.Fill.Color"));
        Assert.Equal("SetImage", VmixScoreboard.FunctionFor("ImgHomeTeam.Source"));
    }
}
