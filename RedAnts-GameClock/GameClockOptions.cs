namespace RedAnts.GameClock;

public sealed class GameClockOptions
{
    public const string Section = "GameClock";

    public int Port { get; set; } = 50085;
    public string? Ip { get; set; }
    public string LogDir { get; set; } = "logs";
    public string DataDir { get; set; } = "data";
    public TeamInfo Home { get; set; } = new();
    public Dictionary<string, TeamInfo> Guests { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TeamInfo
{
    public string Name { get; set; } = "";
    public string Logo { get; set; } = "";
}
