using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Scenes;

public static class GetScenes
{
    public sealed record Query;

    public sealed class Handler(IScenes scenes)
    {
        public async Task<IReadOnlyList<Scene>> HandleAsync(Query query)
        {
            var stored = await scenes.GetAllAsync();
            return DefaultScenes.WithSystem(stored.Count > 0 ? stored : DefaultScenes.All);
        }
    }
}
