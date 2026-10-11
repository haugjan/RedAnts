namespace RedAnts.Show.Features.Agents;

public static class PressDeckKey
{
    public sealed record Command(string HallId, string DeckId, int Number);

    public sealed class Handler(IAgentConnections connections, IAgentChannel channel)
    {
        public async Task<bool> HandleAsync(Command command)
        {
            if (!connections.IsConnected(command.HallId)) return false;
            await channel.PressAsync(command.HallId, command.DeckId, command.Number);
            return true;
        }
    }
}
