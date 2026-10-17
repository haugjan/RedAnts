using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games.Infrastructure;

public sealed class SwissUnihockeyGames(IHttpClientFactory clients) : ISwissUnihockeyGames
{
    public const string ClientName = "swissunihockey";
    private const string Base = "https://api-v2.swissunihockey.ch/api";

    public async Task<IReadOnlyList<UpcomingGame>> UpcomingAsync(int teamId, int season, CancellationToken cancellation = default)
    {
        var json = await Client().GetStringAsync($"{Base}/games?mode=team&team_id={teamId}&season={season}", cancellation);
        return SwissUnihockeyJson.ParseGames(json);
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
