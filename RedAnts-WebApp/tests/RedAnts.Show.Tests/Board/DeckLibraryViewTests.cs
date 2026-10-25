using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Features.Setup;
using Xunit;

namespace RedAnts.Show.Tests.Board;

public class DeckLibraryViewTests
{
    private sealed class NoAgent : IDeckMirror
    {
        public bool IsConnected(string hallId) => false;
        public DeckView? Known(string hallId, string deckId) => null;
        public Task<DeckView?> LiveAsync(string hallId, Deck deck) => Task.FromResult<DeckView?>(null);
        public Task<bool> PressAsync(string hallId, string deckId, int number) => Task.FromResult(false);
        public Task<bool> MusicAsync(string hallId, string musicId) => Task.FromResult(false);
        public void Apply(string hallId, string deckId, string json, Deck deck) { }
        public void ApplyLive(string hallId, string json) { }
        public void Drop(string hallId) { }
    }

    private sealed class Halls(Hall hall) : IHallRepository
    {
        public Task<Hall?> GetByIdAsync(string id) => Task.FromResult(hall.Id == id ? hall : null);
        public Task SaveAsync(Hall saved) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private sealed class Library(IReadOnlyList<MusicItem> items) : IMusicLibrary
    {
        public Task<IReadOnlyList<MusicItem>> AllAsync() => Task.FromResult(items);
        public Task<IReadOnlyList<MusicNode>> TreeAsync() => Task.FromResult<IReadOnlyList<MusicNode>>([]);
    }

    private sealed class Setup(ShowSetup setup) : IShowSetup
    {
        public Task<ShowSetup> GetAsync() => Task.FromResult(setup);
        public Task SaveAsync(ShowSetup saved) => Task.CompletedTask;
    }

    [Fact]
    public async Task The_board_shows_the_deck_of_the_library_not_the_built_in_one()
    {
        var configured = new Deck(DefaultDecks.MusicDeck, "Streamdeck Mk2 (DJ-Pult)", 5, 3,
        [
            new DeckPage(DeckPage.HomeId, "Startseite", null,
            [
                new DeckKey(1, KeyKinds.Page, "Vor/Nach Spiel / Pause", PageId: "vor-nach-spiel"),
                new DeckKey(2, KeyKinds.Music, "Spielunterbruch", MusicId: "break"),
            ]),
            new DeckPage("vor-nach-spiel", "Vor/Nach Spiel / Pause", DeckPage.HomeId, []),
        ], PanicKey: 11, BackKey: 1);
        var hall = Hall.Create("win4-sound", "WIN4 nur Sound") with { Decks = null, DeckIds = [DefaultDecks.MusicDeck] };
        var handler = new GetDeckView.Handler(
            new NoAgent(),
            new Halls(hall),
            new Library([new MusicItem("break", "Spielunterbruch", "", 1, false)]),
            new Setup(new ShowSetup([], [], [configured])));

        var view = await handler.HandleAsync(new GetDeckView.Query(hall.Id, DefaultDecks.MusicDeck));

        Assert.NotNull(view);
        Assert.Equal("Vor/Nach Spiel / Pause", view!.Keys.First(k => k.Number == 1).Label);
        Assert.Equal("break", view.Keys.First(k => k.Number == 2).MusicId);
    }
}
