using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Setup;

public static class SaveShowSetup
{
    public sealed record Command(ShowSetup Setup);

    public sealed class Handler(IShowSetup setup, IHallReader halls, AgentSync sync)
    {
        public async Task<ShowSetup> HandleAsync(Command command)
        {
            await setup.SaveAsync(command.Setup);
            foreach (var hall in await halls.ListAsync())
                await sync.PushAsync(hall.Id);
            return command.Setup;
        }
    }
}
