using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Led;

public static class GuestTeams
{
    public const string Prefix = "gast-";

    sealed record Guest(string Event, int TeamId, IReadOnlyList<string> Names);

    static readonly IReadOnlyList<Guest> Known =
    [
        new("gast-bern", 430340, ["Unihockey Berner Oberland", "Berner Oberland", "BEO"]),
        new("gast-chur", 432516, ["Floorball Chur United", "Chur United"]),
        new("gast-jets", 431166, ["Kloten-Dietlikon Jets", "Jets"]),
        new("gast-laupen", 427952, ["UHC Laupen ZH", "Laupen"]),
        new("gast-riders", 428990, ["FB Riders DBR", "Riders"]),
        new("gast-skorps", 427766, ["UHV Skorpion Emmental", "Skorpion Emmental", "Skorpion"]),
        new("gast-uri", 429324, ["Floorball Uri", "Uri"]),
        new("gast-wizards", 430107, ["Wizards Bern Burgdorf", "Wizards"]),
        new("gast-zug", 429578, ["Zug United", "Zug"]),
    ];

    public static string? EventOf(GameTeam team)
    {
        if (team.TeamId is { } id && Known.FirstOrDefault(guest => guest.TeamId == id) is { } listed) return listed.Event;
        return EventOf(team.LongName) ?? EventOf(team.ShortName);
    }

    public static string? EventOf(string name)
    {
        var words = Words(name);
        if (words.Count == 0) return null;
        var found = Known.Where(guest => guest.Names.Any(alias => Words(alias).IsSubsetOf(words))).ToList();
        return found.Count == 1 ? found[0].Event : null;
    }

    public static string IconOf(string ledEvent) =>
        ledEvent.StartsWith(Prefix, StringComparison.Ordinal) ? "team-" + ledEvent[Prefix.Length..] : ledEvent;

    static HashSet<string> Words(string text) =>
        text.Split(text.Where(c => !char.IsLetterOrDigit(c)).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .ToHashSet();
}
