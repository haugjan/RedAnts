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
            Decks = show.DecksOf(hall),
        };
        return new AgentPayload(configured, game, SwissTime.Timestamp, await music.AllAsync(), await TabsAsync(configured.Decks ?? [], game));
    }

    private async Task<TabBoardPayload?> TabsAsync(IReadOnlyList<Deck> decks, Game? game)
    {
        var board = await boards.GetAsync();
        var variants = (
            from deck in decks
            let tabs = board.DeckFitting(deck)
            where tabs is not null
            from league in board.Leagues
            from tab in BoardTabs.All
            where board.Drives(tabs, tab.Id)
            select new TabDeckVariant(deck.Id, league.Id, tab.Id, board.Compile(tabs, tab.Id, league.Id, deck.Id)))
            .ToList();
        if (variants.Count == 0) return null;
        return new TabBoardPayload(
            board.Leagues.Select(l => new TabChoice(l.Id, l.Name)).ToList(),
            BoardTabs.All.Select(t => new TabChoice(t.Id, t.Label)).ToList(),
            board.LeagueOf(game?.League),
            variants);
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
