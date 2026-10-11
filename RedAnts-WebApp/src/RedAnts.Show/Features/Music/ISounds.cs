using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public interface ISounds
{
    Task<IReadOnlyList<SoundCommand>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<SoundCommand> sounds);
}
