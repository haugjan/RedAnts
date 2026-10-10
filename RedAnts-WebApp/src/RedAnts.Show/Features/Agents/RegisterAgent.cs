using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Agents;

public static class RegisterAgent
{
    public sealed record Command(string HallId, string? Key, string ConnectionId, string Version);

    public sealed class Handler(IHallRepository halls, IAgentConnections connections)
    {
        public async Task<bool> HandleAsync(Command command)
        {
            var hall = await halls.GetByIdAsync(command.HallId);
            if (hall is null || !hall.AcceptsKey(command.Key)) return false;
            connections.Register(new AgentConnection(hall.Id, command.ConnectionId, command.Version, SwissTime.Timestamp));
            return true;
        }
    }
}
