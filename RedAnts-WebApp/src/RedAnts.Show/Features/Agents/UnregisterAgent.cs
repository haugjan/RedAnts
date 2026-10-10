namespace RedAnts.Show.Features.Agents;

public static class UnregisterAgent
{
    public sealed record Command(string ConnectionId);

    public sealed class Handler(IAgentConnections connections)
    {
        public Task<string?> HandleAsync(Command command) => Task.FromResult(connections.Remove(command.ConnectionId));
    }
}
