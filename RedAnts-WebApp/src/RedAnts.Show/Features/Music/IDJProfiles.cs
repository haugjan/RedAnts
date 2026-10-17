using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public interface IDJProfiles
{
    Task<IReadOnlyList<DJProfile>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<DJProfile> profiles);
}
