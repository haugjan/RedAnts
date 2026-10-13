using RedAnts.Show.Agent;
using RedAnts.Show.Agent.Clock;

namespace RedAnts.Show.Agent.Tests;

public class BreakMusicTests
{
    private static ClockState Clock(string time, string mode = ClockState.GameTime, string period = "2") =>
        ICastProtocol.Parse($"{time};3;1;{period};;;;;;;RA;GEG;{mode}")!;

    [Fact]
    public void A_standing_clock_in_the_middle_of_a_period_asks_for_music()
    {
        Assert.True(BreakMusic.Qualifies(Clock("12:34"), clockLive: true, clockRunning: false));
    }

    [Fact]
    public void A_running_clock_never_asks_for_music()
    {
        Assert.False(BreakMusic.Qualifies(Clock("12:34"), clockLive: true, clockRunning: true));
    }

    [Theory]
    [InlineData("00:00", ClockState.GameTime, "3")]
    [InlineData("00:00", ClockState.GameTime, "4")]
    [InlineData("00:00", ClockState.GameTime, "5")]
    [InlineData("0.0", ClockState.GameTime, "3")]
    public void The_end_of_a_period_of_overtime_and_of_the_shootout_stays_quiet(string time, string mode, string period)
    {
        Assert.False(BreakMusic.Qualifies(Clock(time, mode, period), clockLive: true, clockRunning: false));
    }

    [Theory]
    [InlineData(ClockState.Intermission)]
    [InlineData(ClockState.TimeOut)]
    [InlineData("WARM UP")]
    public void A_break_between_the_periods_stays_quiet(string mode)
    {
        Assert.False(BreakMusic.Qualifies(Clock("02:00", mode), clockLive: true, clockRunning: false));
    }

    [Fact]
    public void Without_telegrams_nothing_plays()
    {
        Assert.False(BreakMusic.Qualifies(Clock("12:34"), clockLive: false, clockRunning: false));
    }
}
