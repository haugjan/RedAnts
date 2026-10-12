namespace RedAnts.Show.Features.Agents;

public sealed record AgentPresence(bool Connected, string? Version, DateTimeOffset? ConnectedAt);

public static class GetAgentPresence
{
    public sealed record Query(string HallId);

    public sealed class Handler(IAgentConnections connections)
    {
        public Task<AgentPresence> HandleAsync(Query query)
        {
            var agent = connections.Of(query.HallId);
            return Task.FromResult(new AgentPresence(agent is not null, agent?.Version, agent?.ConnectedAt));
        }
    }
}
