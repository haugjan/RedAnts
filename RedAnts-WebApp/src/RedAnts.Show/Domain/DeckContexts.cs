namespace RedAnts.Show.Domain;

public sealed record DeckContextChoice(string Key, string Label);

public static class DeckContexts
{
    public const string Main = "Main";

    public static IReadOnlyList<DeckContextChoice> All { get; } =
    [
        new(Main, "Startseite"),
        new("GoalHome", "Tor Heim"),
        new("GoalAway", "Tor Gast"),
        new("PlayerHome", "Spielerwahl Heim"),
        new("PlayerAway", "Spielerwahl Gast"),
        new("PenaltyHome", "Strafe Heim"),
        new("PenaltyAway", "Strafe Gast"),
        new("Message", "Mitteilung"),
        new("ScoreHome", "Stand Heim"),
        new("ScoreAway", "Stand Gast"),
    ];

    public static string LabelOf(string? key) =>
        All.FirstOrDefault(c => c.Key == key)?.Label ?? key ?? "";
}
