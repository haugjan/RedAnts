namespace RedAnts.Game.Features.Admin;

public static class GetGameOverview
{
    public sealed record Query;

    public sealed class Handler(IGameOverviewReader overview)
    {
        public Task<GameOverview> HandleAsync(Query query) => overview.GetOverviewAsync();
    }
}
