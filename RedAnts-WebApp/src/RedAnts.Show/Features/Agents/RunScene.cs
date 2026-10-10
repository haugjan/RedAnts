namespace RedAnts.Show.Features.Agents;

public static class RunScene
{
    public sealed record Command(string HallId, string SceneId, IReadOnlyDictionary<string, string>? Args = null);

    public sealed class Handler(IAgentConnections connections, IAgentChannel channel)
    {
        public async Task<bool> HandleAsync(Command command)
        {
            if (!connections.IsConnected(command.HallId)) return false;
            await channel.RunAsync(command.HallId, command.SceneId, command.Args ?? new Dictionary<string, string>());
            return true;
        }
    }
}
