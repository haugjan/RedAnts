using RedAnts.GameClock.Configuration;
using static RedAnts.GameClock.Clocks.ClockFieldReader;

namespace RedAnts.GameClock.Clocks;

public sealed class ICastProtocol : TextProtocol
{
    public const string Id = "icast";

    const int MinimumFields = 13;
    const int ModeIndex = 12;

    static readonly string[] TimeTypes =
    [
        "GAME TIME", "INTERMISSION", "TIME-OUT", "TIME OUT", "WARMUP", "WARM UP",
        "TIME TO WARMUP", "TIME TO FACE-OFF", "LOCAL TIME", "POWERBREAK",
    ];

    public override string Key => Id;

    public override string Name => "iCast Scoreboard";

    public override string Description => "iCast Sweden AB, 10 Telegramme pro Sekunde, Felder mit ; getrennt, UTF-32 Big Endian";

    public override int DefaultPort => 50085;

    public override string DefaultEncoding => LineDecoder.Utf32Be;

    public override ClockState? Parse(string line, ClockSourceConfig config)
    {
        var fields = Split(line);
        if (fields.Length < MinimumFields) return null;

        return new ClockState(
            fields[0], fields[1], fields[2], fields[3],
            At(fields, 10), At(fields, 11), At(fields, ModeIndex),
            ClockState.Penalties(fields[4], fields[5]),
            ClockState.Penalties(fields[6], fields[7]),
            fields, line, DateTime.Now);
    }

    public override int MatchLine(string line)
    {
        var fields = Split(line);
        if (fields.Length < MinimumFields) return 0;

        var score = 40;
        if (LooksLikeClock(fields[0])) score += 20;
        if (LooksLikeScore(fields[1]) && LooksLikeScore(fields[2])) score += 15;
        if (TimeTypes.Contains(fields[ModeIndex], StringComparer.OrdinalIgnoreCase)) score += 25;
        else if (fields[ModeIndex].Any(char.IsAsciiLetter)) score += 10;
        return Math.Min(score, 100);
    }

    static string[] Split(string line) => line.Split(';').Select(x => x.Trim()).ToArray();
}
