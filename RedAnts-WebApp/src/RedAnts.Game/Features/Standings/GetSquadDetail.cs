using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Standings;

public static class GetSquadDetail
{
    public sealed record Query(string ManagerToken);

    public sealed class Handler(IStandingsReader standings, IGameSettings settings)
    {
        public Task<SquadDetailView?> HandleAsync(Query query)
        {
            if (string.IsNullOrWhiteSpace(query.ManagerToken))
                throw new DomainException("Kein Kader angegeben.");

            return standings.GetSquadDetailAsync(query.ManagerToken, settings.Round);
        }
    }
}
