using System.Text.Json;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Players;

public sealed record GamePlayerImportRow(
    int ExternalId,
    string? Name,
    int Position,
    string? Club,
    string? Number,
    int? BirthYear,
    string? Height,
    string? PortraitUrl,
    int MarketValue,
    int Games,
    int Goals,
    int Assists,
    int BestPlayer,
    int PenaltyMinutes,
    decimal GoalsAgainstPerGame,
    int RawTotal,
    decimal RawAverage,
    int PeakPoints,
    int PeakGame,
    decimal RecentAverage,
    int Status,
    string? Licence,
    string? FormCsv,
    Dictionary<string, int>? PointsFrom);

public sealed record GamePlayerImportFile(
    string? Season,
    string? Source,
    string? AsOf,
    IReadOnlyList<GamePlayerImportRow>? Players);

public static class GamePlayerImportMapping
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static GamePlayerImportFile Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DomainException("Es wurden keine Daten übergeben.");

        GamePlayerImportFile? file;
        try
        {
            file = JsonSerializer.Deserialize<GamePlayerImportFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Die Datei ist kein gültiges JSON: {ex.Message}");
        }

        if (file?.Players is null || file.Players.Count == 0)
            throw new DomainException("Die Datei enthält keine Spielerinnen.");

        return file;
    }

    public static List<GamePlayer> ToPlayers(this GamePlayerImportFile file)
    {
        var season = (file.Season ?? "").Trim();
        var rows = file.Players ?? [];
        return rows.Select(row => GamePlayer.Create(
            row.ExternalId,
            row.Name,
            ToPosition(row.Position),
            row.Club,
            row.Number,
            row.BirthYear,
            row.Height,
            row.PortraitUrl,
            row.MarketValue,
            row.Games,
            row.Goals,
            row.Assists,
            row.BestPlayer,
            row.PenaltyMinutes,
            row.GoalsAgainstPerGame,
            row.RawTotal,
            row.RawAverage,
            row.PeakPoints,
            row.PeakGame,
            row.RecentAverage,
            ToStatus(row.Status),
            row.Licence,
            row.FormCsv,
            row.PointsFrom is null ? null : JsonSerializer.Serialize(row.PointsFrom),
            season)).ToList();
    }

    private static PlayerPosition ToPosition(int value) =>
        Enum.IsDefined(typeof(PlayerPosition), value) ? (PlayerPosition)value : PlayerPosition.Forward;

    private static PlayerStatus ToStatus(int value) =>
        Enum.IsDefined(typeof(PlayerStatus), value) ? (PlayerStatus)value : PlayerStatus.Unknown;
}

public static class ImportGamePlayers
{
    public sealed record Command(string Json);

    public sealed record Result(int Imported, string Season, string AsOf);

    public sealed class Handler(IGamePlayerRepository players, IGameSettings settings)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var file = GamePlayerImportMapping.Parse(command.Json);
            var imported = await players.ReplaceAllAsync(file.ToPlayers());

            var season = (file.Season ?? "").Trim();
            if (season.Length > 0) await settings.SetAsync(GameSettingKeys.Season, season);
            var asOf = (file.AsOf ?? "").Trim();
            if (asOf.Length > 0) await settings.SetAsync(GameSettingKeys.AsOf, asOf);

            return new Result(imported, season, asOf);
        }
    }
}
