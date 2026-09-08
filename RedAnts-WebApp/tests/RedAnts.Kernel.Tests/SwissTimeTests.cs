using Xunit;

namespace RedAnts.Kernel.Tests;

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

    [Fact]
    public void Timestamp_carries_the_swiss_offset()
    {
        var stamp = SwissTime.Timestamp;
        Assert.Contains(stamp.Offset, new[] { TimeSpan.FromHours(1), TimeSpan.FromHours(2) });
        Assert.InRange((stamp - DateTimeOffset.UtcNow).Duration().TotalSeconds, 0, 5);
    }

    [Fact]
    public void Winter_offset_instant_converts_to_swiss_wall_clock()
    {
        var instant = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTime(2026, 1, 15, 13, 0, 0), SwissTime.ToSwiss(instant));
        Assert.Equal(TimeSpan.FromHours(1), SwissTime.ToSwissOffset(instant).Offset);
    }

    [Fact]
    public void Summer_offset_instant_converts_to_swiss_wall_clock()
    {
        var instant = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTime(2026, 7, 15, 14, 0, 0), SwissTime.ToSwiss(instant));
        Assert.Equal(TimeSpan.FromHours(2), SwissTime.ToSwissOffset(instant).Offset);
    }

    [Fact]
    public void Utc_and_zurich_offset_inputs_of_the_same_instant_give_the_same_wall_clock()
    {
        var utcInput = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var zurichInput = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.FromHours(2));
        Assert.Equal(SwissTime.ToSwiss(utcInput), SwissTime.ToSwiss(zurichInput));
        Assert.Equal(SwissTime.ToSwissOffset(utcInput), SwissTime.ToSwissOffset(zurichInput));
    }

    [Fact]
    public void Start_of_day_is_swiss_midnight_with_the_seasonal_offset()
    {
        var winter = SwissTime.StartOfDay(new DateOnly(2026, 1, 15));
        Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0), winter.DateTime);
        Assert.Equal(TimeSpan.FromHours(1), winter.Offset);

        var summer = SwissTime.StartOfDay(new DateOnly(2026, 7, 15));
        Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0), summer.DateTime);
        Assert.Equal(TimeSpan.FromHours(2), summer.Offset);
    }
}
