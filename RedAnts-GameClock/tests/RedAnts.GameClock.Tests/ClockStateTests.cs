using RedAnts.GameClock.Clocks;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class ClockStateTests
{
    static ClockState With(string time, string period, string mode) =>
        new(time, "2", "6", period, "RED", "UBO", mode, [], [], [], "", DateTime.Now);

    [Theory]
    [InlineData("1", ClockState.GameTime, "1. DRITTEL")]
    [InlineData("3", ClockState.GameTime, "3. DRITTEL")]
    [InlineData("4", ClockState.GameTime, "VERLÄNGERUNG")]
    [InlineData("5", ClockState.GameTime, "PENALTYSCHIESSEN")]
    [InlineData("2", ClockState.Intermission, "PAUSE")]
    [InlineData("2", ClockState.TimeOut, "TIMEOUT")]
    [InlineData("1", "Time to Face-Off", "BIS SPIELBEGINN")]
    [InlineData("1", "Time to Warmup", "BIS EINLAUFEN")]
    [InlineData("1", "Warmup", "EINLAUFEN")]
    [InlineData("1", "Local Time", "UHRZEIT")]
    [InlineData("", ClockState.GameTime, "")]
    public void NamesThePeriodInGerman(string period, string mode, string expected) =>
        Assert.Equal(expected, With("19:38", period, mode).PeriodLabel);

    [Fact]
    public void TreatsAnEmptyModeAsGameTime()
    {
        var state = With("19:38", "1", "");

        Assert.True(state.IsGameTime);
        Assert.False(state.IsBreak);
        Assert.Equal("1. DRITTEL", state.PeriodLabel);
    }

    [Theory]
    [InlineData("19:38", false)]
    [InlineData("00:04", false)]
    [InlineData("59.9", true)]
    [InlineData("04.2", true)]
    public void RecognisesTheLastMinuteByTheMissingColon(string time, bool expected) =>
        Assert.Equal(expected, With(time, "1", ClockState.GameTime).IsLastMinute);

    [Fact]
    public void StartsWithoutData()
    {
        Assert.False(ClockState.Empty.HasData);
        Assert.Equal("--:--", ClockState.Empty.Time);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("7 01:43", 1)]
    [InlineData("01:43", 1)]
    public void ReadsAPenaltyFieldOnlyWhenItCarriesSomething(string field, int expected) =>
        Assert.Equal(expected, ClockState.Penalties(field).Count);

    [Fact]
    public void SplitsPlayerAndTimeOfAPenalty()
    {
        var penalty = Penalty.Parse("45 01:30");

        Assert.NotNull(penalty);
        Assert.Equal("45", penalty.Player);
        Assert.Equal("01:30", penalty.Time);
    }

    [Fact]
    public void KeepsAPenaltyWithoutAPlayerNumber()
    {
        var penalty = Penalty.Parse("01:30");

        Assert.NotNull(penalty);
        Assert.Equal("", penalty.Player);
        Assert.Equal("01:30", penalty.Time);
    }
}
