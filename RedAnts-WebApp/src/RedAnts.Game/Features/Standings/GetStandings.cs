using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Standings;

public static class GetStandings
{
    public sealed record Query(string ManagerToken);

    public sealed class Handler(IStandingsReader standings, IGameSettings settings)
    {
        public Task<StandingsView> HandleAsync(Query query) =>
            standings.GetStandingsAsync(query.ManagerToken ?? "", settings.Round);
    }
}
