namespace RedAnts.Game.Features.Standings;

public interface IStandingsReader
{
    Task<StandingsView> GetStandingsAsync(string managerToken, int round);

    Task<SquadDetailView?> GetSquadDetailAsync(string managerToken, int round);
}
