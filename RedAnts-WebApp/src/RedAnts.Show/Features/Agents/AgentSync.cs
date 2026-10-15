using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Agents;

public sealed class AgentSync(
    IHallRepository halls,
    IGameRepository games,
    IMusicLibrary music,
    IAgentChannel channel,
    IAgentConnections connections)
{
    public async Task<AgentPayload?> PayloadAsync(string hallId)
    {
        var hall = await halls.GetByIdAsync(hallId);
        if (hall is null) return null;
        var game = hall.ActiveGameId is { } id ? await games.GetByIdAsync(id) : null;
        return new AgentPayload(hall with { AgentKey = "" }, game, SwissTime.Timestamp, await music.AllAsync());
    }

    public async Task PushAsync(string hallId)
    {
        if (!connections.IsConnected(hallId)) return;
        if (await PayloadAsync(hallId) is { } payload)
            await channel.SyncAsync(hallId, payload);
    }
}
