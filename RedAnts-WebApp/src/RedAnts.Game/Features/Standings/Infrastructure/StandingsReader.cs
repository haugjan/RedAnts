using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players.Infrastructure;
using RedAnts.Game.Features.Shared;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Standings.Infrastructure;

public class StandingRosterRow
{
    public string ManagerToken { get; set; } = "";
    public string Name { get; set; } = "";
    public int Slot { get; set; }
    public int MarketValue { get; set; }
    public int? ExternalId { get; set; }
    public string? PlayerName { get; set; }
    public int Position { get; set; }
    public string? Club { get; set; }
    public string? Number { get; set; }
    public string? PortraitUrl { get; set; }
    public int PlayerMarketValue { get; set; }
    public decimal RawAverage { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Games { get; set; }
    public int Status { get; set; }
    public string? FormCsv { get; set; }
}

public sealed class StandingsReader(GameDatabase database) : IStandingsReader
{
    private const string RosterSql = """
        SELECT s.[ManagerToken], s.[Name], ISNULL(sl.[Slot], -1) AS [Slot],
               ISNULL(sl.[MarketValue], 0) AS [MarketValue],
               p.[ExternalId], p.[Name] AS [PlayerName], ISNULL(p.[Position], 0) AS [Position],
               p.[Club], p.[Number], p.[PortraitUrl],
               ISNULL(p.[MarketValue], 0) AS [PlayerMarketValue],
               ISNULL(p.[RawAverage], 0) AS [RawAverage], ISNULL(p.[Goals], 0) AS [Goals],
               ISNULL(p.[Assists], 0) AS [Assists], ISNULL(p.[Games], 0) AS [Games],
               ISNULL(p.[Status], 0) AS [Status], p.[FormCsv]
        FROM [game].[Squads] s
        LEFT JOIN [game].[SquadSlots] sl ON sl.[ManagerToken] = s.[ManagerToken]
        LEFT JOIN [game].[Players] p ON p.[ExternalId] = sl.[PlayerId]
        """;

    public async Task<StandingsView> GetStandingsAsync(string managerToken, int round)
    {
        var rows = await LoadAsync();
        var rounds = SquadScore.RoundsPlayed(rows
            .Where(row => row.FormCsv is not null)
            .Select(row => PlayerCardMapping.ParseForm(row.FormCsv)));

        var ranked = rows
            .GroupBy(row => row.ManagerToken)
            .Select(group => Score(group, round))
            .OrderByDescending(entry => entry.Total)
            .ThenByDescending(entry => entry.Round)
            .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select((entry, index) => new StandingRow(
                entry.Token,
                entry.Name,
                index + 1,
                entry.Round,
                entry.Total,
                entry.Placed,
                entry.Spent,
                entry.Token == managerToken))
            .ToList();

        return new StandingsView(round, rounds, ranked.FirstOrDefault(row => row.IsMine), ranked);
    }

    public async Task<SquadDetailView?> GetSquadDetailAsync(string managerToken, int round)
    {
        var standings = await GetStandingsAsync(managerToken, round);
        var row = standings.Rows.FirstOrDefault(entry => entry.ManagerToken == managerToken);
        if (row is null) return null;

        var rows = (await LoadAsync()).Where(entry => entry.ManagerToken == managerToken).ToList();
        var forms = rows.Where(entry => entry.FormCsv is not null)
            .Select(entry => PlayerCardMapping.ParseForm(entry.FormCsv))
            .ToList();

        var contributions = rows
            .Where(entry => entry.ExternalId is not null)
            .Select(entry =>
            {
                var form = PlayerCardMapping.ParseForm(entry.FormCsv);
                return new SquadContribution(
                    Card(entry),
                    SquadScore.PointsInRound(form, round),
                    form.Take(Math.Max(round, 0)).Sum());
            })
            .OrderByDescending(contribution => contribution.RoundPoints)
            .ThenByDescending(contribution => contribution.TotalPoints)
            .ToList();

        return new SquadDetailView(
            row.Name,
            row.Rank,
            row.RoundPoints,
            row.TotalPoints,
            round,
            SquadScore.PointsByRound(forms, round),
            contributions);
    }

    private Task<List<StandingRosterRow>> LoadAsync() =>
        database.RunAsync(db => db.FetchAsync<StandingRosterRow>(RosterSql));

    private static (string Token, string Name, int Round, int Total, int Placed, int Spent) Score(
        IGrouping<string, StandingRosterRow> group, int round)
    {
        var placed = group.Where(row => row.ExternalId is not null).ToList();
        var forms = placed.Select(row => PlayerCardMapping.ParseForm(row.FormCsv)).ToList();
        return (
            group.Key,
            group.First().Name,
            SquadScore.RoundPoints(forms, round),
            SquadScore.TotalPoints(forms, round),
            placed.Count,
            placed.Sum(row => row.MarketValue));
    }

    private static PlayerCardView Card(StandingRosterRow row)
    {
        var (firstName, lastName) = PlayerName.Split(row.PlayerName);
        return new PlayerCardView(
            row.ExternalId ?? 0,
            firstName,
            lastName,
            (PlayerPosition)row.Position,
            row.Club ?? "",
            row.Number ?? "",
            row.PortraitUrl,
            row.PlayerMarketValue,
            row.RawAverage,
            row.Goals,
            row.Assists,
            row.Games,
            (PlayerStatus)row.Status);
    }
}
