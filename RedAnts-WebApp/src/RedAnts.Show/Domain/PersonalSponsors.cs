namespace RedAnts.Show.Domain;

public sealed record PersonalSponsors(
    string HomeFolder = "",
    string AwayFolder = "",
    IReadOnlyDictionary<string, int>? Home = null,
    IReadOnlyDictionary<string, int>? Away = null,
    bool OnGoal = true,
    bool OnPenalty = true,
    bool OnName = true,
    bool OnBestPlayer = true,
    string BorderColor = PersonalSponsors.DefaultBorder)
{
    public const string DefaultBorder = "#FFF000";
    public const string CoachKey = "coach";
    public const string TopScorerKey = "00";
    public const string HomeSection = "Home";
    public const string AwaySection = "Away";

    public static readonly PersonalSponsors None = new();

    private static readonly IReadOnlyDictionary<string, int> Nothing = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> Counts(string side) => (IsAway(side) ? Away : Home) ?? Nothing;

    public string Folder(string side) => IsAway(side) ? AwayFolder : HomeFolder;

    public int Count(string side, string key) => Counts(side).TryGetValue(key, out var count) ? count : 0;

    public string? Logo(string side, string key, int index = 1) =>
        index >= 1 && Count(side, key) >= index && Folder(side).Length > 0
            ? Join(Folder(side), $"{key}_{index}.png")
            : null;

    public bool Shows(string titleKind) => titleKind.ToLowerInvariant() switch
    {
        "player_event" => OnGoal,
        "penalty" => OnPenalty,
        "name" => OnName,
        "bestplayer" => OnBestPlayer,
        CoachKey => true,
        _ => false,
    };

    public bool Any => Counts("home").Count > 0 || Counts("away").Count > 0;

    public string Ini => string.Join("\n", Lines());

    public static PersonalSponsors Parse(string ini, PersonalSponsors? keep = null)
    {
        var home = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var away = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var current = home;
        foreach (var raw in (ini ?? "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var section = line[1..^1].Trim();
                if (section.Equals(HomeSection, StringComparison.OrdinalIgnoreCase)) current = home;
                else if (section.Equals(AwaySection, StringComparison.OrdinalIgnoreCase)) current = away;
                else throw new ValidationException(nameof(Home), $"Abschnitt „{section}“ unbekannt, erlaubt sind [Home] und [Away].");
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0 || !int.TryParse(line[(eq + 1)..].Trim(), out var count) || count < 0)
                throw new ValidationException(nameof(Home), $"Zeile „{line}“ nicht lesbar, erwartet wird Nummer=Anzahl.");
            current[Key(line[..eq])] = count;
        }
        return (keep ?? None) with { Home = home, Away = away };
    }

    public static string Key(string raw)
    {
        var key = raw.Trim().ToLowerInvariant();
        return key.Length == 2 && key[0] == '0' && key != TopScorerKey ? key[1..] : key;
    }

    public static string Join(string folder, string file)
    {
        var trimmed = folder.TrimEnd('/', '\\');
        var separator = trimmed.Contains('/') && !trimmed.Contains('\\') ? "/" : "\\";
        return trimmed + separator + file;
    }

    private static bool IsAway(string side) => side.Equals("away", StringComparison.OrdinalIgnoreCase);

    private IEnumerable<string> Lines()
    {
        yield return $"[{HomeSection}]";
        foreach (var line in Sorted(Counts("home"))) yield return line;
        yield return $"[{AwaySection}]";
        foreach (var line in Sorted(Counts("away"))) yield return line;
    }

    private static IEnumerable<string> Sorted(IReadOnlyDictionary<string, int> counts) =>
        counts
            .OrderBy(kv => int.TryParse(kv.Key, out _) ? 0 : 1)
            .ThenBy(kv => int.TryParse(kv.Key, out var number) ? number : 0)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");
}
