using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using Xunit;

namespace RedAnts.Show.Tests;

public class DeckViewTests
{
    private sealed class Mirror(DeckView? live, bool connected) : IDeckMirror
    {
        public string? Pressed { get; private set; }

        public bool IsConnected(string hallId) => connected;

        public DeckView? Known(string hallId, string deckId) => null;

        public void Apply(string hallId, string deckId, string json, DeckLayout layout) { }

        public void ApplyLive(string hallId, string json) { }

        public void Drop(string hallId) { }

        public Task<DeckView?> LiveAsync(string hallId, DeckLayout layout) => Task.FromResult(live);

        public string? Played { get; private set; }

        public Task<bool> SoundAsync(string hallId, string soundId)
        {
            Played = soundId;
            return Task.FromResult(connected);
        }

        public Task<bool> PressAsync(string hallId, string deckId, int number)
        {
            Pressed = $"{deckId}/{number}";
            return Task.FromResult(connected);
        }
    }

    private sealed class Halls(Hall? hall) : IHallRepository
    {
        public Task<Hall?> GetByIdAsync(string id) => Task.FromResult(hall);

        public Task SaveAsync(Hall hall) => Task.CompletedTask;

        public Task DeleteAsync(string id) => Task.CompletedTask;
    }

    private sealed class Sounds(params SoundCommand[] sounds) : ISounds
    {
        public Task<IReadOnlyList<SoundCommand>> GetAllAsync() => Task.FromResult<IReadOnlyList<SoundCommand>>(sounds);

        public Task SaveAllAsync(IReadOnlyList<SoundCommand> commands) => Task.CompletedTask;
    }

    private static Hall HallWith(DeckLayout layout) =>
        Hall.Create("win4", "Win4") with { Decks = [layout] };

    private static DeckLayout MusicDeck(string soundId) =>
        new("mk2", "Mk2", 5, 3, [new DeckKey(1, DeckKeyKinds.Sound, "Tor", "#123456", SoundId: soundId)]);

    [Fact]
    public async Task Without_an_agent_the_board_falls_back_to_the_stored_layout()
    {
        var sound = SoundCommand.Create("Tor-Jingle");
        var handler = new GetDeckView.Handler(new Mirror(null, connected: false), new Halls(HallWith(MusicDeck(sound.Id))), new Sounds(sound));

        var view = await handler.HandleAsync(new GetDeckView.Query("win4", "mk2"));

        Assert.NotNull(view);
        Assert.False(view!.FromAgent);
        Assert.Equal(15, view.Keys.Count);
        Assert.Equal(5, view.Columns);
        var first = view.Keys[0];
        Assert.Equal("Tor", first.Label);
        Assert.True(first.Pressable);
        Assert.Equal(sound.Id, first.SoundId);
        Assert.False(view.Keys[1].Pressable);
    }

    [Fact]
    public async Task A_sound_key_without_its_command_cannot_be_pressed()
    {
        var handler = new GetDeckView.Handler(new Mirror(null, connected: false), new Halls(HallWith(MusicDeck("weg"))), new Sounds());

        var view = await handler.HandleAsync(new GetDeckView.Query("win4", "mk2"));

        Assert.False(view!.Keys[0].Pressable);
        Assert.Null(view.Keys[0].SoundId);
    }

    [Fact]
    public async Task With_an_agent_the_board_shows_what_the_agent_reports()
    {
        var live = new DeckView("xl", "Strafe Heim", 8, [new DeckKeyRow(1, "2'", "strafe", "#FF7C80", "#1A1A1A", true, null)], true);
        var handler = new GetDeckView.Handler(new Mirror(live, connected: true), new Halls(HallWith(DefaultDecks.Field())), new Sounds());

        var view = await handler.HandleAsync(new GetDeckView.Query("win4", "xl"));

        Assert.True(view!.FromAgent);
        Assert.Equal("Strafe Heim", view.Title);
        Assert.Single(view.Keys);
    }

    [Fact]
    public async Task An_unknown_hall_has_no_board()
    {
        var handler = new GetDeckView.Handler(new Mirror(null, connected: false), new Halls(null), new Sounds());

        Assert.Null(await handler.HandleAsync(new GetDeckView.Query("fremd", "xl")));
    }

    [Fact]
    public async Task A_press_without_an_agent_is_reported_as_not_sent()
    {
        var mirror = new Mirror(null, connected: false);

        Assert.False(await new PressDeckKey.Handler(mirror).HandleAsync(new PressDeckKey.Command("win4", "xl", 7)));
        Assert.Equal("xl/7", mirror.Pressed);
    }
}
