using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Shared;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Players.Infrastructure;

public sealed class GamePlayerReader(GameDatabase database) : IGamePlayerReader
{
    public async Task<IReadOnlyList<PlayerCardView>> GetMarketAsync(PlayerPosition? position, string? search, int take)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var rows = await database.RunAsync(db => db.FetchAsync<GamePlayerCardRow>(
            $"""
            SELECT TOP (@0) {PlayerCardMapping.CardColumns}
            FROM [game].[Players]
            WHERE (@1 < 0 OR [Position] = @1)
              AND (@2 IS NULL OR [Name] LIKE @2)
            ORDER BY [MarketValue] DESC, [RawAverage] DESC, [Name] ASC
            """,
            take, position is null ? -1 : (int)position, term));
        return rows.Select(row => row.ToCard()).ToList();
    }

    public async Task<IReadOnlyList<PlayerCardView>> GetByIdsAsync(IReadOnlyCollection<int> playerIds)
    {
        if (playerIds.Count == 0) return [];
        var rows = await database.RunAsync(db => db.FetchAsync<GamePlayerCardRow>(
            $"SELECT {PlayerCardMapping.CardColumns} FROM [game].[Players] WHERE [ExternalId] IN (@ids)",
            new { ids = playerIds }));
        return rows.Select(row => row.ToCard()).ToList();
    }

    public async Task<PlayerSheetView?> GetSheetAsync(int playerId)
    {
        var record = await database.RunAsync(db => db.FirstOrDefaultAsync<GamePlayerRecord>(
            "SELECT * FROM [game].[Players] WHERE [ExternalId] = @0", playerId));
        if (record is null) return null;

        var (firstName, lastName) = PlayerName.Split(record.Name);
        var card = new PlayerCardView(
            record.ExternalId,
            firstName,
            lastName,
            (PlayerPosition)record.Position,
            record.Club,
            record.Number,
            record.PortraitUrl,
            record.MarketValue,
            record.RawAverage,
            record.Goals,
            record.Assists,
            record.Games,
            (PlayerStatus)record.Status);

        return new PlayerSheetView(
            card,
            record.BirthYear,
            record.Height,
            record.Licence,
            record.RawTotal,
            record.BestPlayer,
            record.PenaltyMinutes,
            record.GoalsAgainstPerGame,
            record.PeakPoints,
            record.PeakGame,
            record.RecentAverage,
            record.Season,
            PlayerCardMapping.ParseForm(record.FormCsv),
            PlayerCardMapping.ParsePoints(record.PointsJson));
    }
}
