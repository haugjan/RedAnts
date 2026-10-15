using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Halls;

public static class SaveHall
{
    public sealed record Command(Hall Hall);

    public sealed class Handler(IHallRepository halls, AgentSync sync)
    {
        public async Task<Hall> HandleAsync(Command command)
        {
            var stored = await halls.GetByIdAsync(command.Hall.Id)
                ?? throw new DomainException($"Halle „{command.Hall.Id}“ nicht gefunden.");
            var hall = (command.Hall with { AgentKey = stored.AgentKey }).Validated();
            await halls.SaveAsync(hall);
            await sync.PushAsync(hall.Id);
            return hall;
        }
    }
}
