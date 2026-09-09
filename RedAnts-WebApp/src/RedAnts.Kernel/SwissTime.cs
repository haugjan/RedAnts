namespace RedAnts.Domain;

public static class SwissTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime Now => NowOf(null);

    public static DateOnly Today => TodayOf(null);

    public static DateTimeOffset Timestamp => TimestampOf(null);

    public static DateTimeOffset TimestampOf(TimeProvider? time) =>
        TimeZoneInfo.ConvertTime((time ?? TimeProvider.System).GetUtcNow(), Zone);

    public static DateTime NowOf(TimeProvider? time) => TimestampOf(time).DateTime;

    public static DateOnly TodayOf(TimeProvider? time) => DateOnly.FromDateTime(NowOf(time));

    public static DateTime ToSwiss(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime ToSwiss(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Zone).DateTime;

    public static DateTimeOffset ToSwissOffset(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Zone);

    public static DateTimeOffset StartOfDay(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(midnight, Zone.GetUtcOffset(midnight));
    }

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Europe/Zurich", "W. Europe Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
