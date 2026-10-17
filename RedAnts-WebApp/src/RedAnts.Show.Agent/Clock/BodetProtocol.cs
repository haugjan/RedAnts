namespace RedAnts.Show.Agent.Clock;

public static class BodetProtocol
{
    public const string Type = "Bodet";
    public const int DefaultPort = 4001;

    public const byte StartOfHeading = 0x01;
    public const byte StartOfText = 0x02;
    public const byte EndOfText = 0x03;
    public const byte TenthSeparator = (byte)'D';

    public const int MatchMessage = 11;
    public const int HomePenaltyMessage = 12;
    public const int GuestPenaltyMessage = 13;
    public const int ThirdPenaltyMessage = 14;
    public const int PlayerNumberMessage = 15;

    public static bool Supports(string? clockType) =>
        clockType is { Length: > 0 } && clockType.Equals(Type, StringComparison.OrdinalIgnoreCase);
}

public static class BodetFrame
{
    const int HexLimit = 48;

    public static byte Lrc(IReadOnlyList<byte> frame, int count)
    {
        var lrc = 0;
        for (var i = 1; i < count; i++) lrc ^= frame[i];
        lrc &= 0x7F;
        return (byte)(lrc < 0x20 ? lrc + 0x20 : lrc);
    }

    public static bool IsValid(byte[] frame) =>
        frame.Length >= 6
        && frame[0] == BodetProtocol.StartOfHeading
        && frame[2] == BodetProtocol.StartOfText
        && frame[^2] == BodetProtocol.EndOfText
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
            if (data[i] == BodetProtocol.StartOfHeading) start = i;
            else if (data[i] == BodetProtocol.EndOfText && start >= 0 && i + 1 < data.Count)
            {
                frames.Add(data.Skip(start).Take(i + 2 - start).ToArray());
                consumed = i + 2;
                start = -1;
            }
        }
        return frames;
    }

    public static byte At(byte[] frame, int index) => index < frame.Length ? frame[index] : (byte)0x20;

    public static int Digit(byte value) => char.IsAsciiDigit((char)value) ? value - '0' : 0;

    public static int Number(byte[] frame, int from, int count)
    {
        var value = 0;
        for (var i = from; i < from + count; i++) value = value * 10 + Digit(At(frame, i));
        return value;
    }

    public static string Text(byte[] frame, int from, int count)
    {
        var text = new string(Enumerable.Range(from, count).Select(i => (char)At(frame, i)).ToArray()).Trim();
        return text.All(char.IsAsciiDigit) ? text.TrimStart('0') : "";
    }

    public static string Clock(int minutes, int seconds) => $"{minutes:00}:{seconds:00}";

    public static string Hex(byte[] payload) =>
        string.Join(' ', payload.Take(HexLimit).Select(b => b.ToString("x2")))
        + (payload.Length > HexLimit ? $" … ({payload.Length} Bytes)" : "");
}

public sealed record BodetPenalty(string Time, string Player)
{
    public static readonly BodetPenalty None = new("", "");

    public bool Running => Time.Length > 0 && Time != "00:00";
}

public sealed class BodetReader : IClockReader
{
    const int MaximumBuffer = 8192;
    const int Slots = 3;

    readonly List<byte> _buffer = [];
    readonly BodetPenalty[] _home = [BodetPenalty.None, BodetPenalty.None, BodetPenalty.None];
    readonly BodetPenalty[] _guest = [BodetPenalty.None, BodetPenalty.None, BodetPenalty.None];

    string _time = "";
    string _homeScore = "0";
    string _guestScore = "0";
    string _period = "";

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

        return new ClockState(
            _time, _homeScore, _guestScore, _period, "", "", "",
            Penalties(_home), Penalties(_guest),
            string.Join(" | ", valid.Select(BodetFrame.Hex)), DateTime.Now);
    }

    public string Describe(byte[] payload)
    {
        var frames = BodetFrame.Take(payload, out _);
        return frames.Count > 0 ? string.Join(" | ", frames.Select(BodetFrame.Hex)) : BodetFrame.Hex(payload);
    }

    bool Apply(byte[] frame)
    {
        switch (BodetFrame.TypeOf(frame))
        {
            case BodetProtocol.MatchMessage when frame.Length >= 20:
                _time = ReadClock(frame, 8);
                _homeScore = BodetFrame.Number(frame, 12, 3).ToString();
                _guestScore = BodetFrame.Number(frame, 15, 3).ToString();
                _period = BodetFrame.Digit(BodetFrame.At(frame, 18)) is var period && period > 0 ? period.ToString() : "";
                return true;

            case BodetProtocol.HomePenaltyMessage when frame.Length >= 16:
                ReadPenalty(frame, 8, _home, 0);
                ReadPenalty(frame, 12, _home, 1);
                return true;

            case BodetProtocol.GuestPenaltyMessage when frame.Length >= 16:
                ReadPenalty(frame, 8, _guest, 0);
                ReadPenalty(frame, 12, _guest, 1);
                return true;

            case BodetProtocol.ThirdPenaltyMessage when frame.Length >= 16:
                ReadPenalty(frame, 8, _home, 2);
                ReadPenalty(frame, 12, _guest, 2);
                return true;

            case BodetProtocol.PlayerNumberMessage when frame.Length >= 20:
                for (var slot = 0; slot < Slots; slot++)
                {
                    _home[slot] = _home[slot] with { Player = BodetFrame.Text(frame, 7 + slot * 2, 2) };
                    _guest[slot] = _guest[slot] with { Player = BodetFrame.Text(frame, 13 + slot * 2, 2) };
                }
                return true;

            default:
                return false;
        }
    }

    static string ReadClock(byte[] frame, int from) =>
        BodetFrame.At(frame, from + 2) == BodetProtocol.TenthSeparator
            ? $"{(char)BodetFrame.At(frame, from)}{(char)BodetFrame.At(frame, from + 1)}.{(char)BodetFrame.At(frame, from + 3)}"
            : BodetFrame.Clock(BodetFrame.Number(frame, from, 2), BodetFrame.Number(frame, from + 2, 2));

    static void ReadPenalty(byte[] frame, int from, BodetPenalty[] target, int slot) =>
        target[slot] = target[slot] with
        {
            Time = BodetFrame.Clock(BodetFrame.Number(frame, from, 1), BodetFrame.Number(frame, from + 1, 2)),
        };

    static IReadOnlyList<Penalty> Penalties(BodetPenalty[] penalties) =>
        penalties.Where(penalty => penalty.Running)
            .Select(penalty => new Penalty(penalty.Player, penalty.Time))
            .ToList();
}
