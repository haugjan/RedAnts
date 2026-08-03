using RedAnts.Domain;
using Xunit;

namespace RedAnts.Domain.Tests;

public class SwissTimeTests
{
    [Fact]
    public void Summer_instant_is_two_hours_ahead_of_utc()
    {
        var utc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 7, 1, 14, 0, 0), SwissTime.ToSwiss(utc));
    }

    [Fact]
    public void Winter_instant_is_one_hour_ahead_of_utc()
    {
        var utc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 1, 1, 13, 0, 0), SwissTime.ToSwiss(utc));
    }

    [Fact]
    public void Just_after_swiss_midnight_belongs_to_the_new_day()
    {
        var utc = new DateTime(2026, 7, 31, 22, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 8, 1), DateOnly.FromDateTime(SwissTime.ToSwiss(utc)));
    }

    [Fact]
    public void Just_before_swiss_midnight_still_belongs_to_the_old_day()
    {
        var utc = new DateTime(2026, 7, 31, 21, 59, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 7, 31), DateOnly.FromDateTime(SwissTime.ToSwiss(utc)));
    }

    [Fact]
    public void Zone_resolves_and_is_not_utc()
    {
        var offset = SwissTime.Now - DateTime.UtcNow;
        Assert.InRange(offset.TotalHours, 0.9, 2.1);
    }
}
