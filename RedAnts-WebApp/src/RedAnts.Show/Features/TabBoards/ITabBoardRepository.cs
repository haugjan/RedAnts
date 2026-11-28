using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.TabBoards;

public interface ITabBoardRepository
{
    Task<TabBoard> GetAsync();

    Task SaveAsync(TabBoard board);
}
