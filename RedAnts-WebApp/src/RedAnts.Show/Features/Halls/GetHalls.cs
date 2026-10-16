using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Halls;

public sealed record HallRow(string Id, string Name, string? ActiveGameId, bool AgentConnected, bool MusicOnly = false);

public static class GetHalls
{
    public sealed record Query;

    public sealed class Handler(IHallReader reader, IAgentConnections connections)
    {
        public async Task<IReadOnlyList<HallRow>> HandleAsync(Query query)
        {
            var rows = await reader.ListAsync();
            return rows.Select(r => r with { AgentConnected = connections.IsConnected(r.Id) }).ToList();
        }
    }
}
