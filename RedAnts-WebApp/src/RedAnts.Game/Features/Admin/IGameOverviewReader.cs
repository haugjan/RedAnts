namespace RedAnts.Game.Features.Admin;

public interface IGameOverviewReader
{
    Task<GameOverview> GetOverviewAsync();
}
