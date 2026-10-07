namespace RedAnts.GameClock.Vmix;

public enum VmixSlotKind { Text, Color, Image }

public sealed record VmixSlot(string Key, string Label, VmixSlotKind Kind, string Hint = "");

public static class VmixSlots
{
    public const string Time = "time";
    public const string Period = "period";
    public const string HomeScore = "homeScore";
    public const string AwayScore = "awayScore";
    public const string HomeName = "homeName";
    public const string AwayName = "awayName";
    public const string HomePenalty1 = "homePen1";
    public const string HomePenalty2 = "homePen2";
    public const string AwayPenalty1 = "awayPen1";
    public const string AwayPenalty2 = "awayPen2";
    public const string HomePenalty1Fill = "homePen1Fill";
    public const string HomePenalty2Fill = "homePen2Fill";
    public const string AwayPenalty1Fill = "awayPen1Fill";
    public const string AwayPenalty2Fill = "awayPen2Fill";
    public const string HomeLogo = "homeLogo";
    public const string AwayLogo = "awayLogo";

    public const string DigitMarker = "{0}";

    public static readonly string[] DigitPositions = ["M10", "M01", "T", "S10", "S01"];

    public static readonly VmixSlot[] All =
    [
        new(Time, "Spielzeit", VmixSlotKind.Text, "mit {0} je Ziffer, sonst die ganze Zeit in ein Feld"),
        new(Period, "Drittel", VmixSlotKind.Text, "1, 2, 3, O für Verlängerung, P für Penaltyschiessen"),
        new(HomeScore, "Tore Heim", VmixSlotKind.Text),
        new(AwayScore, "Tore Gast", VmixSlotKind.Text),
        new(HomeName, "Name Heim", VmixSlotKind.Text),
        new(AwayName, "Name Gast", VmixSlotKind.Text),
        new(HomePenalty1, "Strafe Heim 1", VmixSlotKind.Text, "mit {0} je Ziffer"),
        new(HomePenalty2, "Strafe Heim 2", VmixSlotKind.Text, "mit {0} je Ziffer"),
        new(AwayPenalty1, "Strafe Gast 1", VmixSlotKind.Text, "mit {0} je Ziffer"),
        new(AwayPenalty2, "Strafe Gast 2", VmixSlotKind.Text, "mit {0} je Ziffer"),
        new(HomePenalty1Fill, "Strafbox Heim 1", VmixSlotKind.Color, "wird bei laufender Strafe sichtbar"),
        new(HomePenalty2Fill, "Strafbox Heim 2", VmixSlotKind.Color),
        new(AwayPenalty1Fill, "Strafbox Gast 1", VmixSlotKind.Color),
        new(AwayPenalty2Fill, "Strafbox Gast 2", VmixSlotKind.Color),
        new(HomeLogo, "Logo Heim", VmixSlotKind.Image, "Dateipfad auf dem vMix-Rechner"),
        new(AwayLogo, "Logo Gast", VmixSlotKind.Image, "Dateipfad auf dem vMix-Rechner"),
    ];

    public static VmixSlot Of(string key) => All.First(s => s.Key == key);

    public static Dictionary<string, string> LuplPreset() => new(StringComparer.OrdinalIgnoreCase)
    {
        [Time] = "TxtClockTime{0}.Text",
        [Period] = "TxtClockPeriod.Text",
        [HomeScore] = "TxtHomeScore.Text",
        [AwayScore] = "TxtAwayScore.Text",
        [HomeName] = "TxtHomeName.Text",
        [AwayName] = "TxtAwayName.Text",
        [HomePenalty1] = "TxtHomePen1{0}.Text",
        [HomePenalty2] = "TxtHomePen2{0}.Text",
        [AwayPenalty1] = "TxtAwayPen1{0}.Text",
        [AwayPenalty2] = "TxtAwayPen2{0}.Text",
        [HomePenalty1Fill] = "RectHomePen1.Fill.Color",
        [HomePenalty2Fill] = "RectHomePen2.Fill.Color",
        [AwayPenalty1Fill] = "RectAwayPen1.Fill.Color",
        [AwayPenalty2Fill] = "RectAwayPen2.Fill.Color",
        [HomeLogo] = "ImgHomeTeam.Source",
        [AwayLogo] = "ImgAwayTeam.Source",
    };

    public const string LuplInput = "LUPL-SU_scoreboard_clock.gtzip";

    public static IEnumerable<(string Field, string Value)> Expand(string field, string value)
    {
        if (!field.Contains(DigitMarker))
        {
            yield return (field, value);
            yield break;
        }

        var padded = value.Length == 0 ? "     " : value.PadRight(DigitPositions.Length);
        for (var i = 0; i < DigitPositions.Length; i++)
            yield return (field.Replace(DigitMarker, DigitPositions[i]), padded[i] is ' ' ? "" : padded[i].ToString());
    }
}
