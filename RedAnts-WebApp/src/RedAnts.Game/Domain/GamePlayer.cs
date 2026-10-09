namespace RedAnts.Game.Domain;

public sealed class GamePlayer
{
    public const int MinMarketValue = 20;
    public const int MaxMarketValue = 150;

    private GamePlayer(int externalId, string name, PlayerPosition position)
    {
        ExternalId = externalId;
        Name = name;
        Position = position;
    }

    public int ExternalId { get; }
    public string Name { get; }
    public PlayerPosition Position { get; }
    public string Club { get; private init; } = "";
    public string Number { get; private init; } = "";
    public int? BirthYear { get; private init; }
    public string? Height { get; private init; }
    public string? PortraitUrl { get; private init; }
    public int MarketValue { get; private init; }
    public int Games { get; private init; }
    public int Goals { get; private init; }
    public int Assists { get; private init; }
    public int BestPlayer { get; private init; }
    public int PenaltyMinutes { get; private init; }
    public decimal GoalsAgainstPerGame { get; private init; }
    public int RawTotal { get; private init; }
    public decimal RawAverage { get; private init; }
    public int PeakPoints { get; private init; }
    public int PeakGame { get; private init; }
    public decimal RecentAverage { get; private init; }
    public PlayerStatus Status { get; private init; }
    public string? Licence { get; private init; }
    public string FormCsv { get; private init; } = "";
    public string? PointsJson { get; private init; }
    public string Season { get; private init; } = "";

    public static GamePlayer Create(
        int externalId,
        string? name,
        PlayerPosition position,
        string? club,
        string? number,
        int? birthYear,
        string? height,
        string? portraitUrl,
        int marketValue,
        int games,
        int goals,
        int assists,
        int bestPlayer,
        int penaltyMinutes,
        decimal goalsAgainstPerGame,
        int rawTotal,
        decimal rawAverage,
        int peakPoints,
        int peakGame,
        decimal recentAverage,
        PlayerStatus status,
        string? licence,
        string? formCsv,
        string? pointsJson,
        string? season)
    {
        if (externalId <= 0) throw new ValidationException(nameof(externalId), "Die Spielerinnen-Id fehlt.");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException(nameof(name), "Der Name fehlt.");
        if (games < 0) throw new ValidationException(nameof(games), "Negative Spiele gibt es nicht.");

        return new GamePlayer(externalId, name.Trim(), position)
        {
            Club = (club ?? "").Trim(),
            Number = (number ?? "").Trim(),
            BirthYear = birthYear,
            Height = Blank(height),
            PortraitUrl = Blank(portraitUrl),
            MarketValue = Math.Clamp(marketValue, MinMarketValue, MaxMarketValue),
            Games = games,
            Goals = goals,
            Assists = assists,
            BestPlayer = bestPlayer,
            PenaltyMinutes = penaltyMinutes,
            GoalsAgainstPerGame = goalsAgainstPerGame,
            RawTotal = rawTotal,
            RawAverage = rawAverage,
            PeakPoints = peakPoints,
            PeakGame = peakGame,
            RecentAverage = recentAverage,
            Status = status,
            Licence = Blank(licence),
            FormCsv = (formCsv ?? "").Trim(),
            PointsJson = Blank(pointsJson),
            Season = (season ?? "").Trim(),
        };
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
