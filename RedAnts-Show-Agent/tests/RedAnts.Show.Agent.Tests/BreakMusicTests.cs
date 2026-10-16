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
        Assert.True(BreakMusic.Qualifies(Clock("12:34"), clockLive: true, clockRunning: false, periodMinutes: 20));
    }

    [Fact]
    public void A_running_clock_never_asks_for_music()
    {
        Assert.False(BreakMusic.Qualifies(Clock("12:34"), clockLive: true, clockRunning: true, periodMinutes: 20));
    }

    [Theory]
    [InlineData("00:00", ClockState.GameTime, "3")]
    [InlineData("00:00", ClockState.GameTime, "4")]
    [InlineData("00:00", ClockState.GameTime, "5")]
    [InlineData("0.0", ClockState.GameTime, "3")]
    public void The_end_of_a_period_of_overtime_and_of_the_shootout_stays_quiet(string time, string mode, string period)
    {
        Assert.False(BreakMusic.Qualifies(Clock(time, mode, period), clockLive: true, clockRunning: false, periodMinutes: 20));
    }

    [Theory]
    [InlineData("20:00")]
    [InlineData("00:00")]
    public void A_clock_resting_at_either_end_of_a_period_stays_quiet(string time)
    {
        Assert.False(BreakMusic.Qualifies(Clock(time), clockLive: true, clockRunning: false, periodMinutes: 20));
        Assert.True(BreakMusic.Qualifies(Clock("19:59"), clockLive: true, clockRunning: false, periodMinutes: 20));
    }

    [Theory]
    [InlineData(ClockState.Intermission)]
    [InlineData(ClockState.TimeOut)]
    [InlineData("WARM UP")]
    public void A_break_between_the_periods_stays_quiet(string mode)
    {
        Assert.False(BreakMusic.Qualifies(Clock("02:00", mode), clockLive: true, clockRunning: false, periodMinutes: 20));
    }

    [Fact]
    public void Music_from_a_key_keeps_the_automatic_quiet()
    {
        var grace = TimeSpan.FromSeconds(12);

        Assert.False(BreakMusic.HeldBack("", "unterbruch", DateTime.MinValue, grace));
        Assert.True(BreakMusic.HeldBack("tor", "unterbruch", DateTime.MinValue, grace));
        Assert.False(BreakMusic.HeldBack("unterbruch", "unterbruch", DateTime.MinValue, grace));
        Assert.True(BreakMusic.HeldBack("", "unterbruch", DateTime.Now, grace));
        Assert.False(BreakMusic.HeldBack("", "unterbruch", DateTime.Now.AddSeconds(-20), grace));
    }

    [Fact]
    public void Without_telegrams_nothing_plays()
    {
        Assert.False(BreakMusic.Qualifies(Clock("12:34"), clockLive: false, clockRunning: false, periodMinutes: 20));
    }
}
