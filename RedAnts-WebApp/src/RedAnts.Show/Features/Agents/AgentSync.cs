using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;

namespace RedAnts.Show.Features.Agents;

public sealed class AgentSync(
    IHallRepository halls,
    IHallReader hallList,
    IGameRepository games,
    IMusicLibrary music,
    IShowSetup setup,
    IAgentChannel channel,
    IAgentConnections connections)
{
    public async Task<AgentPayload?> PayloadAsync(string hallId)
    {
        var hall = await halls.GetByIdAsync(hallId);
        if (hall is null) return null;
        var game = hall.ActiveGameId is { } id ? await games.GetByIdAsync(id) : null;
        var show = await setup.GetAsync();
        var configured = hall with { AgentKey = "", OverlayTitles = show.Titles, LedTemplates = show.Led };
        return new AgentPayload(configured, game, SwissTime.Timestamp, await music.AllAsync());
    }

    public async Task PushAllAsync()
    {
        foreach (var hall in await hallList.ListAsync())
            await PushAsync(hall.Id);
    }

    public async Task PushAsync(string hallId)
    {
        if (!connections.IsConnected(hallId)) return;
        if (await PayloadAsync(hallId) is { } payload)
            await channel.SyncAsync(hallId, payload);
    }
}
