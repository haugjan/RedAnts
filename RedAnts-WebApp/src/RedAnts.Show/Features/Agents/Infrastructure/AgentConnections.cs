using System.Collections.Concurrent;

namespace RedAnts.Show.Features.Agents.Infrastructure;

public sealed class AgentConnections : IAgentConnections
{
    private readonly ConcurrentDictionary<string, AgentConnection> _byConnection = new();

    public void Register(AgentConnection connection) => _byConnection[connection.ConnectionId] = connection;

    public string? Remove(string connectionId) =>
        _byConnection.TryRemove(connectionId, out var removed) ? removed.HallId : null;

    public bool IsConnected(string hallId) => Of(hallId) is not null;

    public AgentConnection? Of(string hallId) =>
        _byConnection.Values.Where(c => c.HallId == hallId).OrderByDescending(c => c.ConnectedAt).FirstOrDefault();
}
