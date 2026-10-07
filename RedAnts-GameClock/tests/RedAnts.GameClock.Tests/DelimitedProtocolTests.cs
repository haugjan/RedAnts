using RedAnts.GameClock.Clocks;
using RedAnts.GameClock.Configuration;
using Xunit;

namespace RedAnts.GameClock.Tests;

public class DelimitedProtocolTests
{
    readonly DelimitedProtocol _protocol = new();

    [Fact]
    public void ReadsTheConfiguredFields()
    {
        var config = new ClockSourceConfig
        {
            Protocol = DelimitedProtocol.Id,
            Layout = new DelimitedLayout
            {
                Separator = ",",
                Time = 1,
                HomeScore = 2,
                GuestScore = 3,
                Period = 0,
                Mode = DelimitedLayout.None,
                HomeAbbreviation = DelimitedLayout.None,
                GuestAbbreviation = DelimitedLayout.None,
                HomePenalty1 = DelimitedLayout.None,
                HomePenalty2 = DelimitedLayout.None,
                GuestPenalty1 = DelimitedLayout.None,
                GuestPenalty2 = DelimitedLayout.None,
            },
        };

        var state = _protocol.Parse("2,11:24,4,3", config);

        Assert.NotNull(state);
        Assert.Equal("11:24", state.Time);
        Assert.Equal("4", state.HomeScore);
        Assert.Equal("3", state.GuestScore);
        Assert.Equal("2", state.Period);
        Assert.Equal("", state.Mode);
        Assert.True(state.IsGameTime);
    }

    [Fact]
    public void RejectsALineWithTooFewFields()
    {
        var config = new ClockSourceConfig { Layout = new DelimitedLayout { Period = 9 } };

        Assert.Null(_protocol.Parse("11:24;4;3", config));
    }

    [Fact]
    public void RejectsALineWithoutATime()
    {
        var config = new ClockSourceConfig { Layout = new DelimitedLayout { Period = DelimitedLayout.None } };

        Assert.Null(_protocol.Parse(";4;3", config));
    }

    [Theory]
    [InlineData(";", ';')]
    [InlineData(",", ',')]
    [InlineData("tab", '\t')]
    [InlineData("space", ' ')]
    [InlineData("", ';')]
    public void ReadsTheSeparatorName(string configured, char expected) =>
        Assert.Equal(expected, DelimitedProtocol.Separator(configured));

    [Fact]
    public void RecognisesAnyLineThatCarriesAClock()
    {
        Assert.True(_protocol.Match("2|11:24|4|3") > 0);
        Assert.True(_protocol.Match("nothing here") < _protocol.Match("2;11:24;4;3"));
    }
}
