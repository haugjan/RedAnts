using RedAnts.GameClock.Configuration;
using static RedAnts.GameClock.Clocks.ClockFieldReader;

namespace RedAnts.GameClock.Clocks;

public sealed class DelimitedProtocol : IClockProtocol
{
    public const string Id = "delimited";

    public static readonly char[] Separators = [';', ',', '\t', '|', ' '];

    public string Key => Id;

    public string Name => "Allgemeine Zeile mit Trennzeichen";

    public string Description => "Jede Uhr, die eine Zeile mit Trennzeichen sendet; Trennzeichen und Feldnummern werden selbst gesetzt";

    public int DefaultPort => 50085;

    public string DefaultEncoding => LineDecoder.Auto;

    public ClockState? Parse(string line, ClockSourceConfig config)
    {
        var layout = config.Layout;
        var fields = line.Split(Separator(layout.Separator)).Select(x => x.Trim()).ToArray();
        if (fields.Length <= layout.HighestIndex) return null;

        var time = At(fields, layout.Time);
        if (time.Length == 0) return null;

        return new ClockState(
            time,
            At(fields, layout.HomeScore),
            At(fields, layout.GuestScore),
            At(fields, layout.Period),
            At(fields, layout.HomeAbbreviation),
            At(fields, layout.GuestAbbreviation),
            At(fields, layout.Mode),
            ClockState.Penalties(At(fields, layout.HomePenalty1), At(fields, layout.HomePenalty2)),
            ClockState.Penalties(At(fields, layout.GuestPenalty1), At(fields, layout.GuestPenalty2)),
            fields, line, DateTime.Now);
    }

    public int Match(string line)
    {
        foreach (var separator in Separators)
        {
            var fields = line.Split(separator);
            if (fields.Length >= 3 && fields.Any(f => LooksLikeClock(f.Trim()))) return 30;
        }
        return line.Length > 0 ? 5 : 0;
    }

    public static char Separator(string configured) => configured switch
    {
        "" => ';',
        "tab" or "\t" => '\t',
        "space" => ' ',
        _ => configured[0],
    };

    public static string SeparatorName(char separator) => separator switch
    {
        '\t' => "tab",
        ' ' => "space",
        _ => separator.ToString(),
    };
}
