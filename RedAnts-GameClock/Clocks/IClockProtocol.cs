using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Clocks;

public interface IClockProtocol
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    int DefaultPort { get; }
    string DefaultEncoding { get; }

    ClockState? Parse(string line, ClockSourceConfig config);

    int Match(string line);
}

public static class ClockFieldReader
{
    public static string At(string[] fields, int index) =>
        index >= 0 && index < fields.Length ? fields[index] : "";

    public static bool LooksLikeClock(string value) =>
        value.Length is >= 3 and <= 8
        && value.Any(char.IsAsciiDigit)
        && value.All(c => char.IsAsciiDigit(c) || c is ':' or '.' or ',');

    public static bool LooksLikeScore(string value) =>
        value.Length is > 0 and <= 3 && value.All(char.IsAsciiDigit);
}
