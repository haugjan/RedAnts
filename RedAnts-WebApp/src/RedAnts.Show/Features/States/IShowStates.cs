using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.States;

public interface IShowStates
{
    Task<IReadOnlyList<ShowState>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<ShowState> states);
}
