using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Players.Infrastructure;

public sealed class GamePlayerRepository(GameDatabase database) : IGamePlayerRepository
{
    public async Task<GamePlayer?> GetByExternalIdAsync(int externalId)
    {
        var record = await database.RunAsync(db => db.FirstOrDefaultAsync<GamePlayerRecord>(
            "SELECT * FROM [game].[Players] WHERE [ExternalId] = @0", externalId));
        return record is null ? null : Map(record);
    }

    public Task<int> CountAsync() => database.RunAsync(db =>
        db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [game].[Players]"));

    public Task<int> ReplaceAllAsync(IReadOnlyList<GamePlayer> players) => database.RunAsync(async db =>
    {
        var now = SwissTime.Timestamp;
        await db.ExecuteAsync("DELETE FROM [game].[Players]");
        foreach (var player in players)
            await db.InsertAsync(ToRecord(player, now));
        return players.Count;
    });

    private static GamePlayerRecord ToRecord(GamePlayer player, DateTimeOffset now) => new()
    {
        ExternalId = player.ExternalId,
        Name = player.Name,
        Position = (int)player.Position,
        Club = player.Club,
        Number = player.Number,
        BirthYear = player.BirthYear,
        Height = player.Height,
        PortraitUrl = player.PortraitUrl,
        MarketValue = player.MarketValue,
        Games = player.Games,
        Goals = player.Goals,
        Assists = player.Assists,
        BestPlayer = player.BestPlayer,
        PenaltyMinutes = player.PenaltyMinutes,
        GoalsAgainstPerGame = player.GoalsAgainstPerGame,
        RawTotal = player.RawTotal,
        RawAverage = player.RawAverage,
        PeakPoints = player.PeakPoints,
        PeakGame = player.PeakGame,
        RecentAverage = player.RecentAverage,
        Status = (int)player.Status,
        Licence = player.Licence,
        FormCsv = player.FormCsv,
        PointsJson = player.PointsJson,
        Season = player.Season,
        UpdatedAt = now,
    };

    private static GamePlayer Map(GamePlayerRecord record) => GamePlayer.Create(
        record.ExternalId,
        record.Name,
        (PlayerPosition)record.Position,
        record.Club,
        record.Number,
        record.BirthYear,
        record.Height,
        record.PortraitUrl,
        record.MarketValue,
        record.Games,
        record.Goals,
        record.Assists,
        record.BestPlayer,
        record.PenaltyMinutes,
        record.GoalsAgainstPerGame,
        record.RawTotal,
        record.RawAverage,
        record.PeakPoints,
        record.PeakGame,
        record.RecentAverage,
        (PlayerStatus)record.Status,
        record.Licence,
        record.FormCsv,
        record.PointsJson,
        record.Season);
}
