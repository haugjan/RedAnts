using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Board;

public interface IDJProfiles
{
    Task<IReadOnlyList<DJProfile>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<DJProfile> profiles);
}
