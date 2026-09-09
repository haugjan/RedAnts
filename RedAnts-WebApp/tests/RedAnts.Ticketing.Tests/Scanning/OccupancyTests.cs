using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using Xunit;

namespace RedAnts.Ticketing.Tests.Scanning;

public class OccupancyTests
{
    [Fact]
    public void Remaining_is_quota_minus_inside_floored_at_zero()
    {
        Assert.Equal(60, new Occupancy(40, 100).Remaining);
        Assert.Equal(0, new Occupancy(120, 100).Remaining);
    }

    [Fact]
    public void Remaining_is_null_without_quota() =>
        Assert.Null(new Occupancy(40, null).Remaining);

    [Theory]
    [InlineData(99, 100, false)]
    [InlineData(100, 100, true)]
    [InlineData(150, 100, true)]
    public void Full_when_inside_reaches_the_quota(int inside, int quota, bool expected) =>
        Assert.Equal(expected, new Occupancy(inside, quota).Full);

    [Fact]
    public void Never_full_without_quota() =>
        Assert.False(new Occupancy(5000, null).Full);

    [Fact]
    public void Tally_for_returns_the_known_tally_or_an_empty_one()
    {
        var occupancy = new Occupancy(10, 100, 3, [new FreeEntryTally(FreeEntryType.Player, 2, 1)]);

        var player = occupancy.TallyFor(FreeEntryType.Player);
        var child = occupancy.TallyFor(FreeEntryType.Child);

        Assert.Equal(2, player.Inside);
        Assert.Equal(1, player.Out);
        Assert.Equal(FreeEntryType.Child, child.Type);
        Assert.Equal(0, child.Inside);
        Assert.Equal(0, child.Out);
    }

    [Fact]
    public void Tallies_are_empty_when_none_were_given()
    {
        var occupancy = new Occupancy(1, 2);

        Assert.Empty(occupancy.Tallies);
        Assert.Equal(0, occupancy.FreeInside);
    }
}
