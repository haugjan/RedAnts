using System.Globalization;
using System.Text.Json;
using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games.Infrastructure;

public static class SwissUnihockeyJson
{
    public static IReadOnlyList<UpcomingGame> ParseGames(string json)
    {
        using var document = JsonDocument.Parse(json);
        var games = new List<UpcomingGame>();
        foreach (var row in Rows(document.RootElement))
        {
            if (!row.TryGetProperty("link", out var link) || !link.TryGetProperty("ids", out var ids) || ids.GetArrayLength() == 0) continue;
            var cells = row.GetProperty("cells");
            var when = Texts(cells[0]);
            games.Add(new UpcomingGame(
                ids[0].GetRawText(),
                ParseDate(when.ElementAtOrDefault(0), when.ElementAtOrDefault(1)),
                Texts(cells[2]).FirstOrDefault() ?? "",
                Texts(cells[3]).FirstOrDefault() ?? "",
                string.Join(", ", Texts(cells[1])),
                Texts(cells[4]).FirstOrDefault() ?? ""));
        }
        return games;
    }

    public static Game ParseGame(string gameId, string detailJson, string homeLineupJson, string awayLineupJson)
    {
        using var detail = JsonDocument.Parse(detailJson);
        var data = detail.RootElement.GetProperty("data");
        var keys = data.GetProperty("headers").EnumerateArray().Select(h => h.GetProperty("key").GetString() ?? "").ToList();
        var cells = Rows(detail.RootElement).First().GetProperty("cells").EnumerateArray().ToList();

        JsonElement? Cell(string key) => keys.IndexOf(key) is var index && index >= 0 && index < cells.Count ? cells[index] : null;
        string Text(string key) => Cell(key) is { } cell ? Texts(cell).FirstOrDefault() ?? "" : "";
        string? Image(string key) => Cell(key) is { } cell && cell.TryGetProperty("image", out var image) ? image.GetProperty("url").GetString() : null;
        int? TeamId(string key) => Cell(key) is { } cell && cell.TryGetProperty("link", out var link) && link.TryGetProperty("ids", out var ids) && ids.GetArrayLength() > 0 ? ids[0].GetInt32() : null;

        var home = Team(TeamId("home_name"), Text("home_name"), Image("home_logo"), GameTeam.HomeColor, homeLineupJson);
        var away = Team(TeamId("away_name"), Text("away_name"), Image("away_logo"), GameTeam.AwayColor, awayLineupJson);
        var title = data.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "" : "";
        var subtitle = data.TryGetProperty("subtitle", out var subtitleElement) ? subtitleElement.GetString() ?? "" : "";
        return new Game(
            gameId,
            GameSource.SwissUnihockey,
            title.Length > 0 ? title : $"{home.LongName} - {away.LongName}",
            subtitle,
            ParseDate(Text("date"), Text("time")),
            Text("location"),
            home,
            away,
            Text("first_referee"),
            Text("second_referee"),
            "",
            "",
            SwissTime.Timestamp);
    }

    private static GameTeam Team(int? id, string longName, string? logo, string color, string lineupJson)
    {
        using var lineup = JsonDocument.Parse(lineupJson);
        var players = new List<Player>();
        foreach (var row in Rows(lineup.RootElement))
        {
            var cells = row.GetProperty("cells");
            var number = Texts(cells[0]).FirstOrDefault();
            var name = Texts(cells[2]).FirstOrDefault() ?? "";
            if (!int.TryParse(number, out var parsed) || name.Length == 0) continue;
            players.Add(new Player(parsed, name, Texts(cells[1]).FirstOrDefault() ?? ""));
        }
        var goalies = players
            .Where(p => p.Position.StartsWith("Goalie", StringComparison.OrdinalIgnoreCase) || p.Position.StartsWith("Torh", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Number)
            .ToList();
        return new GameTeam(id, longName, GameTeam.ShortNameOf(longName), color, logo, "", players.OrderBy(p => p.Number).ToList(), [], [], goalies);
    }

    private static IEnumerable<JsonElement> Rows(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("regions", out var regions)) yield break;
        foreach (var region in regions.EnumerateArray())
            if (region.TryGetProperty("rows", out var rows))
                foreach (var row in rows.EnumerateArray()) yield return row;
    }

    private static List<string> Texts(JsonElement cell)
    {
        if (!cell.TryGetProperty("text", out var text)) return [];
        if (text.ValueKind == JsonValueKind.String) return [text.GetString() ?? ""];
        if (text.ValueKind != JsonValueKind.Array) return [];
        return text.EnumerateArray()
            .Where(t => t.ValueKind == JsonValueKind.String)
            .Select(t => t.GetString() ?? "")
            .Where(t => t.Length > 0)
            .ToList();
    }

    private static DateTime? ParseDate(string? date, string? time)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        var text = $"{date.Trim()} {(string.IsNullOrWhiteSpace(time) ? "00:00" : time.Trim())}";
        return DateTime.TryParseExact(text, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    }
}
