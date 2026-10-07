using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Clocks;

public sealed class BodetScorepadProtocol : IClockProtocol
{
    public const string Id = "bodet";

    public const byte StartOfHeading = 0x01;
    public const byte StartOfText = 0x02;
    public const byte EndOfText = 0x03;

    public const int MatchMessage = 11;
    public const int HomePenaltyMessage = 12;
    public const int GuestPenaltyMessage = 13;

    public string Key => Id;

    public string Name => "Bodet ScorePad";

    public string Description => "Bodet ScorePad über TCP, Rahmen mit SOH/STX/ETX und LRC; die Uhr verbindet sich auf diesen Port";

    public int DefaultPort => 4001;

    public string DefaultEncoding => LineDecoder.Auto;

    public ClockTransport Transport => ClockTransport.Tcp;

    public IClockParser CreateParser(ClockSourceConfig config) => new BodetParser();

    public int Match(byte[] payload)
    {
        var valid = BodetFrame.Take(payload, out _).Where(BodetFrame.IsValid).ToArray();
        if (valid.Length == 0) return 0;

        var known = valid.Count(f => BodetFrame.TypeOf(f) is MatchMessage or HomePenaltyMessage or GuestPenaltyMessage);
        return Math.Min(60 + known * 20, 100);
    }
}

public static class BodetFrame
{
    public static byte Lrc(IReadOnlyList<byte> frame, int count)
    {
        var lrc = 0;
        for (var i = 1; i < count; i++) lrc ^= frame[i];
        lrc &= 0x7F;
        return (byte)(lrc < 0x20 ? lrc + 0x20 : lrc);
    }

    public static bool IsValid(byte[] frame) =>
        frame.Length >= 6
        && frame[0] == BodetScorepadProtocol.StartOfHeading
        && frame[^2] == BodetScorepadProtocol.EndOfText
        && frame[^1] == Lrc(frame, frame.Length - 1);

    public static int TypeOf(byte[] frame) =>
        frame.Length > 5 && char.IsAsciiDigit((char)frame[4]) && char.IsAsciiDigit((char)frame[5])
            ? (frame[4] - '0') * 10 + (frame[5] - '0')
            : -1;

    public static IReadOnlyList<byte[]> Take(IReadOnlyList<byte> data, out int consumed)
    {
        var frames = new List<byte[]>();
        var start = -1;
        consumed = 0;

        for (var i = 0; i < data.Count; i++)
        {
            if (data[i] == BodetScorepadProtocol.StartOfHeading) start = i;
            else if (data[i] == BodetScorepadProtocol.EndOfText && start >= 0 && i + 1 < data.Count)
            {
                frames.Add(data.Skip(start).Take(i + 2 - start).ToArray());
                consumed = i + 2;
                start = -1;
            }
        }
        return frames;
    }

    public static int Digit(byte value) => value == 0x20 ? 0 : char.IsAsciiDigit((char)value) ? value - '0' : 0;

    public static int Number(byte[] frame, int from, int count)
    {
        var value = 0;
        for (var i = from; i < from + count; i++)
            value = value * 10 + (i < frame.Length ? Digit(frame[i]) : 0);
        return value;
    }

    public static string Clock(int minutes, int seconds) => $"{minutes:00}:{seconds:00}";
}

public sealed class BodetParser : IClockParser
{
    const int MaximumBuffer = 8192;
    const string NoPenalty = "00:00";

    readonly List<byte> _buffer = [];
    readonly string[] _home = ["", ""];
    readonly string[] _guest = ["", ""];

    string _time = "";
    string _homeScore = "0";
    string _guestScore = "0";
    string _period = "";
    string _raw = "";

    public ClockState? Read(byte[] payload)
    {
        _buffer.AddRange(payload);
        if (_buffer.Count > MaximumBuffer) _buffer.RemoveRange(0, _buffer.Count - MaximumBuffer);

        var frames = BodetFrame.Take(_buffer, out var consumed);
        _buffer.RemoveRange(0, consumed);
        if (frames.Count == 0) return null;

        var valid = frames.Where(BodetFrame.IsValid).ToArray();
        var seen = false;
        foreach (var frame in valid) seen |= Apply(frame);
        if (!seen || _time.Length == 0) return null;

        _raw = string.Join(" | ", valid.Select(ClockFieldReader.Hex));

        return new ClockState(
            _time, _homeScore, _guestScore, _period, "", "", "",
            Penalties(_home), Penalties(_guest),
            [_time, _homeScore, _guestScore, _period], _raw, DateTime.Now);
    }

    public string Describe(byte[] payload)
    {
        var frames = BodetFrame.Take(payload, out _);
        return frames.Count > 0
            ? string.Join(" | ", frames.Select(ClockFieldReader.Hex))
            : ClockFieldReader.Hex(payload);
    }

    bool Apply(byte[] frame)
    {
        switch (BodetFrame.TypeOf(frame))
        {
            case BodetScorepadProtocol.MatchMessage when frame.Length >= 20:
                _time = BodetFrame.Clock(BodetFrame.Number(frame, 8, 2), BodetFrame.Number(frame, 10, 2));
                _homeScore = BodetFrame.Number(frame, 12, 3).ToString();
                _guestScore = BodetFrame.Number(frame, 15, 3).ToString();
                _period = BodetFrame.Digit(frame[18]) is var period && period > 0 ? period.ToString() : "";
                return true;

            case BodetScorepadProtocol.HomePenaltyMessage when frame.Length >= 16:
                ReadPenalties(frame, _home);
                return true;

            case BodetScorepadProtocol.GuestPenaltyMessage when frame.Length >= 16:
                ReadPenalties(frame, _guest);
                return true;

            default:
                return false;
        }
    }

    static void ReadPenalties(byte[] frame, string[] target)
    {
        target[0] = BodetFrame.Clock(BodetFrame.Number(frame, 8, 1), BodetFrame.Number(frame, 9, 2));
        target[1] = BodetFrame.Clock(BodetFrame.Number(frame, 12, 1), BodetFrame.Number(frame, 13, 2));
    }

    static IReadOnlyList<Penalty> Penalties(string[] times) =>
        times.Where(time => time.Length > 0 && time != NoPenalty)
            .Select(time => new Penalty("", time))
            .ToList();
}
