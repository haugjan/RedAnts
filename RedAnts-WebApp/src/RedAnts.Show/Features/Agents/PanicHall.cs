namespace RedAnts.Show.Features.Agents;

public static class PanicHall
{
    public sealed record Command(string HallId);

    public sealed class Handler(IAgentConnections connections, IAgentChannel channel)
    {
        public async Task<bool> HandleAsync(Command command)
        {
            if (!connections.IsConnected(command.HallId)) return false;
            await channel.PanicAsync(command.HallId);
            return true;
        }
    }
}
