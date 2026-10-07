using System.Text;

namespace RedAnts.GameClock.Teams;

public static class TeamMatching
{
    static readonly string[] ClubWords =
    [
        "UHC", "UHT", "UHV", "SV", "HC", "FB", "FBC", "SC", "TV", "CLUB", "TEAM",
        "FLOORBALL", "UNIHOCKEY",
    ];

    public static IReadOnlyList<DirectoryTeam> Rank(string abbreviation, IEnumerable<DirectoryTeam> teams) =>
        teams.Select(team => (Team: team, Score: Score(abbreviation, team.Name)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Team.Name, StringComparer.CurrentCulture)
            .Select(x => x.Team)
            .ToList();

    public static int Score(string abbreviation, string teamName)
    {
        var wanted = Normalize(abbreviation);
        if (wanted.Length == 0) return 0;

        var candidates = Candidates(teamName);
        if (candidates.Contains(wanted)) return 100;
        if (candidates.Any(c => c.StartsWith(wanted, StringComparison.Ordinal))) return 70;
        if (candidates.Any(c => wanted.StartsWith(c, StringComparison.Ordinal) && c.Length >= 2)) return 55;

        var letters = Normalize(teamName);
        if (letters.Contains(wanted, StringComparison.Ordinal)) return 40;
        return InOrder(wanted, letters) ? 20 : 0;
    }

    public static HashSet<string> Candidates(string teamName)
    {
        var words = Words(teamName);
        var significant = words.Where(w => !ClubWords.Contains(w, StringComparer.Ordinal)).ToArray();
        if (significant.Length == 0) significant = words;

        var candidates = new HashSet<string>(StringComparer.Ordinal);
        Add(candidates, Initials(words));
        Add(candidates, Initials(significant));
        Add(candidates, LeadingPair(words));
        Add(candidates, LeadingPair(significant));
        Add(candidates, string.Concat(words));
        Add(candidates, string.Concat(significant));
        foreach (var length in new[] { 2, 3, 4 })
        {
            Add(candidates, Prefix(words.FirstOrDefault(), length));
            Add(candidates, Prefix(significant.FirstOrDefault(), length));
            Add(candidates, Prefix(string.Concat(significant), length));
        }
        return candidates;
    }

    static void Add(HashSet<string> candidates, string? value)
    {
        if (value is { Length: >= 2 }) candidates.Add(value);
    }

    static string Prefix(string? word, int length) =>
        word is null || word.Length < length ? "" : word[..length];

    static string Initials(IEnumerable<string> words) =>
        string.Concat(words.Where(w => w.Length > 0).Select(w => w[0]));

    static string LeadingPair(string[] words) =>
        words.Length < 2 || words[0].Length < 2 ? "" : words[0][..2] + Initials(words[1..]);

    static string[] Words(string teamName) =>
        Strip(teamName)
            .Split([' ', '-', '.', '/', '\'', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

    public static string Normalize(string value) => string.Concat(Strip(value).Where(char.IsAsciiLetterOrDigit));

    static readonly Dictionary<char, string> Folded = new()
    {
        ['Ä'] = "AE", ['Ö'] = "OE", ['Ü'] = "UE", ['ß'] = "SS",
        ['À'] = "A", ['Á'] = "A", ['Â'] = "A", ['Ã'] = "A", ['Å'] = "A",
        ['È'] = "E", ['É'] = "E", ['Ê'] = "E", ['Ë'] = "E",
        ['Ì'] = "I", ['Í'] = "I", ['Î'] = "I", ['Ï'] = "I",
        ['Ò'] = "O", ['Ó'] = "O", ['Ô'] = "O", ['Õ'] = "O", ['Ø'] = "O",
        ['Ù'] = "U", ['Ú'] = "U", ['Û'] = "U",
        ['Ç'] = "C", ['Ñ'] = "N", ['Ý'] = "Y",
    };

    static string Strip(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            var upper = char.ToUpperInvariant(character);
            builder.Append(Folded.TryGetValue(upper, out var replacement) ? replacement : upper.ToString());
        }
        return builder.ToString();
    }

    static bool InOrder(string wanted, string letters)
    {
        var position = 0;
        foreach (var c in wanted)
        {
            position = letters.IndexOf(c, position);
            if (position < 0) return false;
            position++;
        }
        return true;
    }
}
