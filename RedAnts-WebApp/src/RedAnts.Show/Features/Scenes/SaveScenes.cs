using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Scenes;

public static class SaveScenes
{
    public sealed record Command(IReadOnlyList<Scene> Scenes);

    public sealed class Handler(IScenes scenes, IHallReader halls, AgentSync sync)
    {
        public async Task HandleAsync(Command command)
        {
            var validated = command.Scenes.Where(s => !s.System).Select(s => s.Validated()).ToList();
            var duplicate = validated.GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null) throw new ValidationException("Id", $"Szene „{duplicate.Key}“ ist doppelt.");
            await scenes.SaveAllAsync(validated);
            foreach (var hall in await halls.ListAsync())
                await sync.PushAsync(hall.Id);
        }
    }
}
