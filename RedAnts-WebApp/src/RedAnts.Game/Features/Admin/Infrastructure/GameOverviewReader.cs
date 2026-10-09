using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Admin.Infrastructure;

public class AdminPlayerListRow
{
    public int ExternalId { get; set; }
    public string Name { get; set; } = "";
    public int Position { get; set; }
    public string Club { get; set; } = "";
    public int MarketValue { get; set; }
    public decimal RawAverage { get; set; }
    public int Games { get; set; }
    public int Status { get; set; }
    public string? PortraitUrl { get; set; }
    public string Season { get; set; } = "";
}

public class AdminSquadListRow
{
    public string ManagerToken { get; set; } = "";
    public string Name { get; set; } = "";
    public int Placed { get; set; }
    public int Spent { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class GameOverviewReader(GameDatabase database) : IGameOverviewReader
{
    public async Task<GameOverview> GetOverviewAsync()
    {
        var (players, squads) = await database.RunAsync<(List<AdminPlayerListRow> Players, List<AdminSquadListRow> Squads)>(async db =>
        {
            var rows = await db.FetchAsync<AdminPlayerListRow>(
                """
                SELECT [ExternalId],[Name],[Position],[Club],[MarketValue],[RawAverage],[Games],[Status],[PortraitUrl],[Season]
                FROM [game].[Players]
                ORDER BY [MarketValue] DESC, [Name] ASC
                """);
            var teams = await db.FetchAsync<AdminSquadListRow>(
                """
                SELECT s.[ManagerToken], s.[Name], s.[UpdatedAt],
                       COUNT(p.[Slot]) AS [Placed],
                       ISNULL(SUM(p.[MarketValue]), 0) AS [Spent]
                FROM [game].[Squads] s
                LEFT JOIN [game].[SquadSlots] p ON p.[ManagerToken] = s.[ManagerToken]
                GROUP BY s.[ManagerToken], s.[Name], s.[UpdatedAt]
                ORDER BY s.[UpdatedAt] DESC
                """);
            return (rows, teams);
        });

        return new GameOverview(
            players.Count,
            players.Count(row => !string.IsNullOrEmpty(row.PortraitUrl)),
            squads.Count,
            players.Select(row => row.Season).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "",
            "",
            players.Select(row => new AdminPlayerRow(
                row.ExternalId,
                row.Name,
                (PlayerPosition)row.Position,
                row.Club,
                row.MarketValue,
                row.RawAverage,
                row.Games,
                (PlayerStatus)row.Status,
                !string.IsNullOrEmpty(row.PortraitUrl))).ToList(),
            squads.Select(row => new AdminSquadRow(
                row.ManagerToken,
                row.Name,
                row.Placed,
                row.Spent,
                row.UpdatedAt)).ToList());
    }
}
