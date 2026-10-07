namespace RedAnts.GameClock.Configuration;

public sealed class GameClockConfig
{
    public bool Configured { get; set; }
    public ClockSourceConfig Clock { get; set; } = new();
    public VmixConfig Vmix { get; set; } = new();
    public TeamConfig Teams { get; set; } = new();

    public GameClockConfig Copy() => new()
    {
        Configured = Configured,
        Clock = Clock.Copy(),
        Vmix = Vmix.Copy(),
        Teams = Teams.Copy(),
    };
}

public sealed class ClockSourceConfig
{
    public string Protocol { get; set; } = "icast";
    public int Port { get; set; } = 50085;
    public string? SourceIp { get; set; }
    public string Encoding { get; set; } = "auto";
    public DelimitedLayout Layout { get; set; } = new();

    public bool AcceptsAnySource => string.IsNullOrWhiteSpace(SourceIp) || SourceIp.Equals("any", StringComparison.OrdinalIgnoreCase);

    public ClockSourceConfig Copy() => new()
    {
        Protocol = Protocol,
        Port = Port,
        SourceIp = SourceIp,
        Encoding = Encoding,
        Layout = Layout.Copy(),
    };
}

public sealed class DelimitedLayout
{
    public const int None = -1;

    public string Separator { get; set; } = ";";
    public int Time { get; set; } = 0;
    public int HomeScore { get; set; } = 1;
    public int GuestScore { get; set; } = 2;
    public int Period { get; set; } = 3;
    public int HomePenalty1 { get; set; } = None;
    public int HomePenalty2 { get; set; } = None;
    public int GuestPenalty1 { get; set; } = None;
    public int GuestPenalty2 { get; set; } = None;
    public int HomeAbbreviation { get; set; } = None;
    public int GuestAbbreviation { get; set; } = None;
    public int Mode { get; set; } = None;

    public int HighestIndex => new[]
    {
        Time, HomeScore, GuestScore, Period, HomePenalty1, HomePenalty2,
        GuestPenalty1, GuestPenalty2, HomeAbbreviation, GuestAbbreviation, Mode,
    }.Max();

    public DelimitedLayout Copy() => (DelimitedLayout)MemberwiseClone();
}

public sealed class VmixConfig
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8099;
    public string Input { get; set; } = Vmix.VmixSlots.LuplInput;
    public bool SendLogos { get; set; }
    public string PenaltyFill { get; set; } = "#141414";
    public Dictionary<string, string> Fields { get; set; } = Vmix.VmixSlots.LuplPreset();

    public VmixConfig Copy() => new()
    {
        Enabled = Enabled,
        Host = Host,
        Port = Port,
        Input = Input,
        SendLogos = SendLogos,
        PenaltyFill = PenaltyFill,
        Fields = new Dictionary<string, string>(Fields, StringComparer.OrdinalIgnoreCase),
    };
}

public sealed class TeamConfig
{
    public int Season { get; set; } = 2026;
    public int GameClass { get; set; } = 21;
    public TeamEntry Home { get; set; } = new();
    public Dictionary<string, TeamEntry> ByAbbreviation { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TeamEntry? Find(string abbreviation) =>
        abbreviation.Length > 0 && ByAbbreviation.TryGetValue(abbreviation, out var team) ? team : null;

    public TeamConfig Copy() => new()
    {
        Season = Season,
        GameClass = GameClass,
        Home = Home.Copy(),
        ByAbbreviation = ByAbbreviation.ToDictionary(e => e.Key, e => e.Value.Copy(), StringComparer.OrdinalIgnoreCase),
    };
}

public sealed class TeamEntry
{
    public string Name { get; set; } = "";
    public string Logo { get; set; } = "";

    public TeamEntry Copy() => (TeamEntry)MemberwiseClone();
}
