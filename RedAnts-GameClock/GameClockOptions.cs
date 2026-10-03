namespace RedAnts.GameClock;

public sealed class GameClockOptions
{
    public const string Section = "GameClock";

    public int Port { get; set; } = 50085;
    public string? Ip { get; set; }
    public string LogDir { get; set; } = "logs";
    public TeamInfo Home { get; set; } = new();
    public Dictionary<string, TeamInfo> Guests { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TeamInfo? FindGuest(string abbreviation) =>
        Guests.FirstOrDefault(g => string.Equals(g.Key, abbreviation, StringComparison.OrdinalIgnoreCase)).Value;
}

public sealed class TeamInfo
{
    public string Name { get; set; } = "";
    public string Logo { get; set; } = "";
}
