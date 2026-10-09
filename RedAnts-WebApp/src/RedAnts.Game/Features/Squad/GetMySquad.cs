using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Features.Squad;

public static class GetMySquad
{
    public sealed record Query(string ManagerToken);

    public sealed class Handler(ISquadReader squads, IGameSettings settings)
    {
        public Task<SquadView> HandleAsync(Query query)
        {
            if (string.IsNullOrWhiteSpace(query.ManagerToken))
                throw new DomainException("Kein Managerkennzeichen.");

            return squads.GetViewAsync(query.ManagerToken, settings.Budget);
        }
    }
}
