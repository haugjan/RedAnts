using System.Text.Json;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Players.Infrastructure;

public class GamePlayerCardRow
{
    public int ExternalId { get; set; }
    public string Name { get; set; } = "";
    public int Position { get; set; }
    public string Club { get; set; } = "";
    public string Number { get; set; } = "";
    public string? PortraitUrl { get; set; }
    public int MarketValue { get; set; }
    public decimal RawAverage { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Games { get; set; }
    public int Status { get; set; }
    public int BestPlayer { get; set; }
    public int PeakPoints { get; set; }
    public int PeakGame { get; set; }
    public decimal RecentAverage { get; set; }
    public decimal GoalsAgainstPerGame { get; set; }
}

public static class PlayerCardMapping
{
    public const string CardColumns =
        "[ExternalId],[Name],[Position],[Club],[Number],[PortraitUrl],[MarketValue],[RawAverage]," +
        "[Goals],[Assists],[Games],[Status],[BestPlayer],[PeakPoints],[PeakGame],[RecentAverage],[GoalsAgainstPerGame]";

    public static PlayerCardView ToCard(this GamePlayerCardRow row)
    {
        var (firstName, lastName) = PlayerName.Split(row.Name);
        return new PlayerCardView(
            row.ExternalId,
            firstName,
            lastName,
            (PlayerPosition)row.Position,
            row.Club,
            row.Number,
            row.PortraitUrl,
            row.MarketValue,
            row.RawAverage,
            row.Goals,
            row.Assists,
            row.Games,
            (PlayerStatus)row.Status);
    }

    public static IReadOnlyList<int> ParseForm(string? formCsv)
    {
        if (string.IsNullOrWhiteSpace(formCsv)) return [];
        var points = new List<int>();
        foreach (var part in formCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var value)) points.Add(value);
        return points;
    }

    public static IReadOnlyList<PointsOriginRow> ParsePoints(string? pointsJson)
    {
        if (string.IsNullOrWhiteSpace(pointsJson)) return [];
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, int>>(pointsJson);
            if (parsed is null) return [];
            return parsed
                .Where(pair => pair.Value != 0)
                .OrderBy(pair => PointsOrigin.Order(pair.Key))
                .Select(pair => new PointsOriginRow(PointsOrigin.Label(pair.Key), pair.Value))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
