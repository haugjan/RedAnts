using RedAnts.Show.Features.Games;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Setup;
using RedAnts.Show.Features.TabBoards;

namespace RedAnts.Show.Features.Agents;

public sealed class AgentSync(
    IHallRepository halls,
    IHallReader hallList,
    IGameRepository games,
    IMusicLibrary music,
    IShowSetup setup,
    ITabBoardRepository boards,
    IBoardSessionRepository sessions,
    IAgentChannel channel,
    IAgentConnections connections)
{
    public async Task<AgentPayload?> PayloadAsync(string hallId)
    {
        var hall = await halls.GetByIdAsync(hallId);
        if (hall is null) return null;
        var game = hall.ActiveGameId is { } id ? await games.GetByIdAsync(id) : null;
        var show = await setup.GetAsync();
        var configured = hall with
        {
            AgentKey = "",
            OverlayTitles = show.Titles,
            LedTemplates = show.Led,
            Decks = await DecksAsync(hall, show, game),
        };
        return new AgentPayload(configured, game, SwissTime.Timestamp, await music.AllAsync());
    }

    private async Task<IReadOnlyList<Deck>> DecksAsync(Hall hall, ShowSetup show, Game? game)
    {
        var decks = show.DecksOf(hall);
        if (await sessions.GetAsync(hall.Id) is not { Active: true } session) return decks;
        var board = await boards.GetAsync();
        var league = board.Leagues.Any(l => l.Id == session.League) ? session.League : board.LeagueOf(game?.League);
        return decks.Select(deck => board.DeckFitting(deck) is { } tabs && board.Drives(tabs, session.Tab)
                ? board.Compile(tabs, session.Tab, league, deck.Id)
                : deck)
            .ToList();
    }

    public async Task PushAllAsync()
    {
        foreach (var hall in await hallList.ListAsync())
            await PushAsync(hall.Id);
    }

    public async Task PushGameAsync(string gameId)
    {
        foreach (var hall in await hallList.ListAsync())
            if (hall.ActiveGameId == gameId) await PushAsync(hall.Id);
    }

    public async Task PushAsync(string hallId)
    {
        if (!connections.IsConnected(hallId)) return;
        if (await PayloadAsync(hallId) is { } payload)
            await channel.SyncAsync(hallId, payload);
    }
}
