using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Setup;

public static class SaveShowSetup
{
    public sealed record Command(ShowSetup Setup);

    public sealed class Handler(IShowSetup setup, AgentSync sync)
    {
        public async Task<ShowSetup> HandleAsync(Command command)
        {
            await setup.SaveAsync(command.Setup);
            await sync.PushAllAsync();
            return command.Setup;
        }
    }
}
