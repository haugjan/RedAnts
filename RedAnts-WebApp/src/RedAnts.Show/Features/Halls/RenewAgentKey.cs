using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Halls;

public static class RenewAgentKey
{
    public sealed record Command(string HallId);

    public sealed class Handler(IHallRepository halls)
    {
        public async Task<Hall> HandleAsync(Command command)
        {
            var hall = await halls.GetByIdAsync(command.HallId)
                ?? throw new DomainException($"Halle „{command.HallId}“ nicht gefunden.");
            var renewed = hall.WithNewKey();
            await halls.SaveAsync(renewed);
            return renewed;
        }
    }
}
