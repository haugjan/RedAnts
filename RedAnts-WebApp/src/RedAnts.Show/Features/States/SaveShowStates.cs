using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.States;

public static class SaveShowStates
{
    public sealed record Command(IReadOnlyList<ShowState> States);

    public sealed class Handler(IShowStates states, IHallReader halls, AgentSync sync)
    {
        public async Task HandleAsync(Command command)
        {
            var validated = command.States.Select(s => s.Validated()).ToList();
            var duplicate = validated.GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null) throw new ValidationException("Id", $"Zustand „{duplicate.Key}“ ist doppelt.");
            await states.SaveAllAsync(validated);
            foreach (var hall in await halls.ListAsync())
                await sync.PushAsync(hall.Id);
        }
    }
}
