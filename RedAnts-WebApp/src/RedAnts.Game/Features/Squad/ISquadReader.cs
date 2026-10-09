namespace RedAnts.Game.Features.Squad;

public interface ISquadReader
{
    Task<SquadView> GetViewAsync(string managerToken, int budget);
}
