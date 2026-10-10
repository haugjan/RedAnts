namespace RedAnts.Show.Features.Agents;

public static class GetAgentPayload
{
    public sealed record Query(string HallId);

    public sealed class Handler(AgentSync sync)
    {
        public Task<AgentPayload?> HandleAsync(Query query) => sync.PayloadAsync(query.HallId);
    }
}
