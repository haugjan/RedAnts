using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Clocks;

public enum ClockTransport { Udp, Tcp }

public interface IClockProtocol
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    int DefaultPort { get; }
    string DefaultEncoding { get; }
    ClockTransport Transport { get; }

    IClockParser CreateParser(ClockSourceConfig config);

    int Match(byte[] payload);
}

public interface IClockParser
{
    ClockState? Read(byte[] payload);

    string Describe(byte[] payload);
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

    public static string Hex(byte[] payload, int limit = 48) =>
        string.Join(' ', payload.Take(limit).Select(b => b.ToString("x2")))
        + (payload.Length > limit ? $" … ({payload.Length} Bytes)" : "");
}

public abstract class TextProtocol : IClockProtocol
{
    public abstract string Key { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract int DefaultPort { get; }
    public abstract string DefaultEncoding { get; }

    public virtual ClockTransport Transport => ClockTransport.Udp;

    public IClockParser CreateParser(ClockSourceConfig config) => new TextParser(this, config);

    public int Match(byte[] payload) => MatchLine(LineDecoder.Decode(payload, LineDecoder.Auto));

    public abstract ClockState? Parse(string line, ClockSourceConfig config);

    public abstract int MatchLine(string line);

    sealed class TextParser(TextProtocol protocol, ClockSourceConfig config) : IClockParser
    {
        public ClockState? Read(byte[] payload) => protocol.Parse(Describe(payload), config);

        public string Describe(byte[] payload) => LineDecoder.Decode(payload, config.Encoding);
    }
}
