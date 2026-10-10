using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Scenes;

public interface IScenes
{
    Task<IReadOnlyList<Scene>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<Scene> scenes);
}
