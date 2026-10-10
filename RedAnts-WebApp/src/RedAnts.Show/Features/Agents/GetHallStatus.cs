using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Agents;

public sealed record HallStatus(bool Connected, string? AgentVersion, DateTimeOffset? ConnectedAt, IReadOnlyList<AgentReport> Reports)
{
    public AgentReport? Report(string kind) => Reports.FirstOrDefault(r => r.Kind == kind);
}

public static class GetHallStatus
{
    public sealed record Query(string HallId);

    public sealed class Handler(IAgentConnections connections, IAgentReports reports)
    {
        public async Task<HallStatus> HandleAsync(Query query)
        {
            var connection = connections.Of(query.HallId);
            return new HallStatus(connection is not null, connection?.Version, connection?.ConnectedAt, await reports.LatestAsync(query.HallId));
        }
    }
}
