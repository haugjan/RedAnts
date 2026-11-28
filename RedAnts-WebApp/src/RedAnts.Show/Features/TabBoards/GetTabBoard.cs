using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.TabBoards;

public static class GetTabBoard
{
    public sealed record Query;

    public sealed class Handler(ITabBoardRepository boards)
    {
        public Task<TabBoard> HandleAsync(Query query) => boards.GetAsync();
    }
}
