using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Halls;

public static class SetActiveGame
{
    public sealed record Command(string HallId, string? GameId);

    public sealed class Handler(IHallRepository halls, AgentSync sync)
    {
        public async Task<Hall> HandleAsync(Command command)
        {
            var hall = await halls.GetByIdAsync(command.HallId)
                ?? throw new DomainException($"Halle „{command.HallId}“ nicht gefunden.");
            var updated = hall.WithActiveGame(command.GameId);
            await halls.SaveAsync(updated);
            await sync.PushAsync(updated.Id);
            return updated;
        }
    }
}
