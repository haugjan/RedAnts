using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games.Infrastructure;

public sealed class SwissUnihockeyGames(IHttpClientFactory clients) : ISwissUnihockeyGames
{
    public const string ClientName = "swissunihockey";
    private const string Base = "https://api-v2.swissunihockey.ch/api";

    public async Task<IReadOnlyList<UpcomingGame>> ClubHomeGamesAsync(int season, CancellationToken cancellation = default)
    {
        var client = Client();
        var teamsJson = await client.GetStringAsync($"{Base}/teams?mode=by_club&club_id={ClubTeams.RedAntsClubId}&season={season}", cancellation);
        var teams = ClubTeams.Pick(SwissUnihockeyJson.ParseTeams(teamsJson));
        var lists = await Task.WhenAll(teams.Select(async team =>
        {
            var json = await client.GetStringAsync($"{Base}/games?mode=team&team_id={team.Id}&season={season}&games_per_page=200", cancellation);
            return SwissUnihockeyJson.ParseGames(json).Select(game => game with { League = team.League });
        }));
        return lists
            .SelectMany(games => games)
            .Where(ClubTeams.IsHome)
            .DistinctBy(game => game.Id)
            .ToList();
    }

    public async Task<Game> LoadAsync(string gameId, CancellationToken cancellation = default)
    {
        var client = Client();
        var id = Uri.EscapeDataString(gameId);
        var detail = client.GetStringAsync($"{Base}/games/{id}", cancellation);
        var home = client.GetStringAsync($"{Base}/games/{id}/teams/0/players", cancellation);
        var away = client.GetStringAsync($"{Base}/games/{id}/teams/1/players", cancellation);
        await Task.WhenAll(detail, home, away);
        return SwissUnihockeyJson.ParseGame(gameId, detail.Result, home.Result, away.Result);
    }

    private HttpClient Client()
    {
        var client = clients.CreateClient(ClientName);
        client.Timeout = TimeSpan.FromSeconds(20);
        return client;
    }
}
