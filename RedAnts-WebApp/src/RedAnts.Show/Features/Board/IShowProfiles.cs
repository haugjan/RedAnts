using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Board;

public interface IShowProfiles
{
    Task<IReadOnlyList<ShowProfile>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles);
}
