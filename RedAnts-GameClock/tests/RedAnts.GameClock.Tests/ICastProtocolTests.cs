using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Configuration;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class ICastProtocolTests
{
    const string Hall = "19:38;2;6;3;;;;;0;0;RED;UBO;GAME TIME;0;-6;0;-2;0;0;1";
    const string Manual = "10:22;3;2;1;5 01:21;45 01:30;7 01:43;87 01:51;0;0;HOM;GUE;GAME TIME";

    readonly ICastProtocol _protocol = new();
    readonly ClockSourceConfig _config = new();

    [Fact]
    public void ReadsTheHallTelegram()
    {
        var state = _protocol.Parse(Hall, _config);

        Assert.NotNull(state);
        Assert.Equal("19:38", state.Time);
        Assert.Equal("2", state.HomeScore);
        Assert.Equal("6", state.GuestScore);
        Assert.Equal("3", state.Period);
        Assert.Equal("RED", state.Home);
        Assert.Equal("UBO", state.Guest);
        Assert.Equal(ClockState.GameTime, state.Mode);
        Assert.Empty(state.HomePenalties);
        Assert.Empty(state.GuestPenalties);
    }

    [Fact]
    public void ReadsThePenaltiesOfTheManualSample()
    {
        var state = _protocol.Parse(Manual, _config);

        Assert.NotNull(state);
        Assert.Collection(state.HomePenalties,
            first => Assert.Equal(new Penalty("5", "01:21"), first),
            second => Assert.Equal(new Penalty("45", "01:30"), second));
        Assert.Collection(state.GuestPenalties,
            first => Assert.Equal(new Penalty("7", "01:43"), first),
            second => Assert.Equal(new Penalty("87", "01:51"), second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("19:38;2;6")]
    [InlineData("19:38;2;6;3;;;;;0;0;RED;UBO")]
    public void RejectsShortTelegrams(string line) => Assert.Null(_protocol.Parse(line, _config));

    [Fact]
    public void ScoresAKnownTimeTypeHighest()
    {
        var known = _protocol.Match(Hall);
        var unknown = _protocol.Match("19:38;2;6;3;;;;;0;0;RED;UBO;SOMETHING");

        Assert.Equal(100, known);
        Assert.InRange(unknown, 1, 99);
        Assert.Equal(0, _protocol.Match("hello world"));
    }

    [Fact]
    public void KeepsAllRawFieldsForTheLog()
    {
        var state = _protocol.Parse(Hall, _config);

        Assert.NotNull(state);
        Assert.Equal(20, state.Fields.Length);
        Assert.Equal(Hall, state.Raw);
    }
}
