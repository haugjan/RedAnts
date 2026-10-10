using System.Globalization;

namespace RedAnts.Show.Domain;

public static class GameConfigFile
{
    public static Game Parse(string text)
    {
        var sections = Sections(text);
        var game = sections.GetValueOrDefault("game") ?? [];
        var id = Value(game, "GameNr");
        ValidationException.ThrowIfBlank(id, "Id", "GameNr");
        return new Game(
            id.Trim(),
            GameSource.File,
            Value(game, "Title"),
            Value(game, "LeagueId"),
            ParseDate(Value(game, "Date"), Value(game, "Time")),
            Value(game, "Location"),
            Team(sections.GetValueOrDefault("home"), GameTeam.HomeColor),
            Team(sections.GetValueOrDefault("away"), GameTeam.AwayColor),
            Value(game, "Referee1"),
            Value(game, "Referee2"),
            Value(game, "Commentator1"),
            Value(game, "Commentator2"),
            SwissTime.Timestamp).Validated();
    }

    private static GameTeam Team(List<(string Key, string Value)>? section, string color)
    {
        if (section is null) return GameTeam.Empty(color);
        var players = section
            .Where(kv => int.TryParse(kv.Key, out _))
            .Select(kv => new Player(int.Parse(kv.Key, CultureInfo.InvariantCulture), kv.Value.Trim()))
            .Where(p => p.Name.Length > 0)
            .OrderBy(p => p.Number)
            .ToList();
        var longName = Value(section, "TeamLong");
        var shortName = Value(section, "TeamShort");
        return new GameTeam(
            null,
            longName,
            shortName.Length > 0 ? shortName : GameTeam.ShortNameOf(longName),
            color,
            null,
            Value(section, "Coach"),
            players,
            Numbers(Value(section, "Starting6")),
            Enumerable.Range(1, 4).Select(i => Numbers(Value(section, $"Line{i}"))).Where(l => l.Count > 0).ToList(),
            Numbers(Value(section, "Goal")));
    }

    private static string Value(List<(string Key, string Value)> section, string key) =>
        section.LastOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value ?? "";

    private static IReadOnlyList<int> Numbers(string value) =>
        value
            .Split(',', StringSplitOptions.TrimEntries)
            .Select(n => int.TryParse(n, out var number) ? number : -1)
            .Where(n => n >= 0)
            .ToList();

    private static DateTime? ParseDate(string date, string time)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        var text = $"{date.Trim()} {(string.IsNullOrWhiteSpace(time) ? "00:00" : time.Trim())}";
        return DateTime.TryParseExact(text, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private static Dictionary<string, List<(string Key, string Value)>> Sections(string text)
    {
        var sections = new Dictionary<string, List<(string Key, string Value)>>(StringComparer.OrdinalIgnoreCase);
        List<(string Key, string Value)>? current = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = [];
                sections[line[1..^1]] = current;
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0 || current is null) continue;
            current.Add((line[..eq].Trim(), line[(eq + 1)..].Trim()));
        }
        return sections;
    }
}
