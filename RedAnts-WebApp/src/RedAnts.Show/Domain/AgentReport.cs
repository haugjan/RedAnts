namespace RedAnts.Show.Domain;

public sealed record AgentReport(string Kind, string Json, DateTimeOffset At);

public static class AgentReportKinds
{
    public const string Status = "status";
    public const string Score = "score";
    public const string Clock = "clock";
    public const string Overlay = "overlay";
    public const string Deck = "deck";
    public const string Log = "log";
    public const string Sound = "sound";
}
