namespace RedAnts.Show.Features.Agents;

public sealed record AgentConnection(string HallId, string ConnectionId, string Version, DateTimeOffset ConnectedAt);

public interface IAgentConnections
{
    void Register(AgentConnection connection);
    string? Remove(string connectionId);
    bool IsConnected(string hallId);
    AgentConnection? Of(string hallId);
}
