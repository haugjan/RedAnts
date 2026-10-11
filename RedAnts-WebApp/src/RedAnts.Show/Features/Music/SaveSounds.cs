using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Music;

public static class SaveSounds
{
    public sealed record Command(IReadOnlyList<SoundCommand> Sounds);

    public sealed class Handler(ISounds sounds, IHallReader halls, AgentSync sync)
    {
        public async Task HandleAsync(Command command)
        {
            var validated = command.Sounds.Select(s => s.Validated()).ToList();
            var duplicate = validated.GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null) throw new ValidationException("Id", $"Musik-Befehl „{duplicate.Key}“ ist doppelt.");
            await sounds.SaveAllAsync(validated);
            foreach (var hall in await halls.ListAsync())
                await sync.PushAsync(hall.Id);
        }
    }
}
