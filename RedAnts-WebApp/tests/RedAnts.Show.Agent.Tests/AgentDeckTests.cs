using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class AgentDeckTests
{
    const string DeckId = "mk2";
    const string NewLine = "\n";

    static readonly MusicItem Goal = new("tor", "Tor Red Ants", "L-UPL", 1, false, "[{\"t\":\"local\",\"r\":\"/dj/sound/tor.m4a\"}]");
    static readonly MusicItem Break = new("unterbruch", "Spielunterbruch", "L-UPL", 91, true, "[]");

    static Deck.Deck Keys(out PayloadStore store, out ShowStates states, params DeckKey[] keys)
    {
        var deck = new DeckLayout(DeckId, "Mk2", 3, 2, [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)]);
        return Build(out store, out states, deck);
    }

    static Deck.Deck Build(out PayloadStore store, out ShowStates states, params DeckLayout[] decks) =>
        Build(null, out store, out states, decks);

    static Deck.Deck Build(HallCapabilities? offers, out PayloadStore store, out ShowStates states, params DeckLayout[] decks)
    {
        store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with { Id = "win4", Name = "Win4", Decks = decks, BreakMusicId = Break.Id, Offered = offers };
        store.Apply(new AgentPayload(hall, Game.None, DateTimeOffset.Now, [Goal, Break]));
        states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var vmix = new VmixLink(store, live, NullLogger<VmixLink>.Instance);
        var led = new SednaLink(store, NullLogger<SednaLink>.Instance);
        var runner = new KeyRunner(store, live, states, vmix, led, NullLogger<KeyRunner>.Instance);
        return new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance);
    }

    [Fact]
    public void A_hall_with_sound_only_keeps_the_music_keys_and_hides_what_it_cannot_run()
    {
        var layout = new DeckLayout(DeckId, "Mk2", 3, 2, [new DeckPage(DeckPage.HomeId, "Startseite", null,
        [
            new DeckKey(1, KeyKinds.Music, "Spielunterbruch", MusicId: Break.Id),
            new DeckKey(2, KeyKinds.Special, "Automodus", Special: SpecialKeys.AutoBreakMusic),
            new DeckKey(3, KeyKinds.Special, "Stopp", Special: SpecialKeys.MusicStop),
            new DeckKey(4, KeyKinds.Special, "Tor", Special: SpecialKeys.GoalHome),
        ])]);
        var deck = Build(new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: false), out _, out _, layout);

        var slots = deck.Slots(DeckId).ToDictionary(slot => slot.Number);

        Assert.Equal("Spielunterbruch", slots[1].Label);
        Assert.False(slots[2].Pressable);
        Assert.True(slots[3].Pressable);
        Assert.False(slots[4].Pressable);
    }

    [Fact]
    public void A_sequence_runs_the_steps_the_hall_offers_and_stays_on_the_deck()
    {
        var layout = new DeckLayout(DeckId, "Mk2", 3, 2, [new DeckPage(DeckPage.HomeId, "Startseite", null,
        [
            new DeckKey(1, KeyKinds.Steps, "Tor mit Jingle", Steps:
            [
                new KeyStep(KeyActions.MusicPlay, Goal.Id),
                new KeyStep(KeyActions.VmixShow, "title:goal"),
            ]),
            new DeckKey(2, KeyKinds.Steps, "Opener", Steps: [new KeyStep(KeyActions.VmixShow, "title:opener")]),
        ])]);
        var deck = Build(new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: false), out _, out _, layout);

        var slots = deck.Slots(DeckId).ToDictionary(slot => slot.Number);

        Assert.True(slots[1].Pressable);
        Assert.False(slots[2].Pressable);
    }

    [Fact]
    public async Task The_player_list_shortens_long_names_to_the_first_name_and_an_initial()
    {
        var layout = new DeckLayout(DeckId, "Mk2", 4, 4,
            [new DeckPage(DeckPage.HomeId, "Startseite", null, [new DeckKey(1, KeyKinds.Special, "Tor", Special: SpecialKeys.GoalHome)])]);
        var deck = Build(out var store, out _, layout);
        var team = GameTeam.Empty with
        {
            ShortName = "RA",
            Players = [new Player(7, "Anna Habersaat"), new Player(9, "Ida Sundberg"), new Player(11, "Mirjamvonnebenan")],
        };
        store.Apply(new AgentPayload(store.Hall, Game.None with { Id = "1", Home = team }, DateTimeOffset.Now, []));

        await deck.PressAsync(DeckId, 1);
        var labels = deck.Slots(DeckId).Select(s => s.Label).ToList();

        Assert.Contains("7" + NewLine + "Anna H.", labels);
        Assert.Contains("9" + NewLine + "Ida Sundberg", labels);
        Assert.Contains("11" + NewLine + "Mirjamvonneb", labels);
    }

    [Fact]
    public async Task A_second_press_on_a_running_piece_fades_it_out_instead_of_cutting_it()
    {
        var deck = Keys(out _, out var states, new DeckKey(1, KeyKinds.Music, "", MusicId: Goal.Id));

        await deck.PressAsync(DeckId, 1);
        Assert.Equal(Goal.Id, deck.MusicId);

        await deck.PressAsync(DeckId, 1);

        Assert.Equal("fade", deck.ControlWanted);
        Assert.Equal("", deck.MusicId);
        Assert.Equal("aus", states.Value(ShowStates.Music));
    }

    [Fact]
    public void Panic_sits_on_its_place_and_the_rest_of_the_grid_is_filled()
    {
        var deck = Keys(out _, out _);

        var slots = deck.Slots(DeckId);

        Assert.Equal(6, slots.Count);
        Assert.Equal("🚨 PANIC", slots[5].Label);
        Assert.True(slots[5].Pressable);
    }

    [Fact]
    public void Back_stays_dark_on_the_home_page()
    {
        var deck = Keys(out _, out _);

        var back = deck.Slots(DeckId)[3];

        Assert.False(back.Pressable);
        Assert.Equal("", back.Label);
    }

    [Fact]
    public void A_music_key_carries_the_command_and_the_name_of_the_piece()
    {
        var deck = Keys(out _, out _, new DeckKey(1, KeyKinds.Music, "", MusicId: Goal.Id));

        var slot = deck.Slots(DeckId)[0];

        Assert.Equal("Tor Red Ants", slot.Label);
        Assert.Equal(Goal.Id, slot.MusicId);
        Assert.True(slot.Pressable);
    }

    [Fact]
    public void A_music_key_whose_command_is_gone_is_dead()
    {
        var deck = Keys(out _, out _, new DeckKey(1, KeyKinds.Music, "Tor", MusicId: "weg"));

        Assert.False(deck.Slots(DeckId)[0].Pressable);
    }

    [Fact]
    public async Task Pressing_a_music_key_twice_starts_and_stops_it()
    {
        var deck = Keys(out _, out _, new DeckKey(1, KeyKinds.Music, "", MusicId: Goal.Id));

        await deck.PressAsync(DeckId, 1);
        var running = deck.Slots(DeckId)[0];

        await deck.PressAsync(DeckId, 1);
        var stopped = deck.Slots(DeckId)[0];

        Assert.True(running.Active);
        Assert.False(stopped.Active);
    }

    [Fact]
    public async Task A_page_carries_the_name_of_the_key_that_opened_it_and_back_returns()
    {
        var deck = new DeckLayout(DeckId, "Mk2", 3, 2,
        [
            new DeckPage(DeckPage.HomeId, "Startseite", null,
            [
                new DeckKey(1, KeyKinds.Page, "Mehr", PageId: "mehr"),
                new DeckKey(2, KeyKinds.Page, "Pause L-UPL", PageId: "mehr"),
            ]),
            new DeckPage("mehr", "Mehr Musik", DeckPage.HomeId, [new DeckKey(1, KeyKinds.Music, "", MusicId: Break.Id)]),
        ]);
        var board = Build(out _, out _, deck);

        await board.PressAsync(DeckId, 1);
        Assert.Equal("Mehr", board.Title(DeckId));
        Assert.Equal("Mehr", board.Slots(DeckId)[3].Top);
        Assert.True(board.Slots(DeckId)[3].Pressable);

        await board.PressAsync(DeckId, 4);
        Assert.Equal("Startseite", board.Title(DeckId));

        await board.PressAsync(DeckId, 2);
        Assert.Equal("Pause L-UPL", board.Title(DeckId));
        Assert.Equal("Pause L-UPL", board.Slots(DeckId)[3].Top);
    }

    [Fact]
    public async Task Back_walks_the_way_it_came_through_several_pages()
    {
        var deck = new DeckLayout(DeckId, "Mk2", 3, 2,
        [
            new DeckPage(DeckPage.HomeId, "Startseite", null, [new DeckKey(1, KeyKinds.Page, "Liga", PageId: "liga")]),
            new DeckPage("liga", "Liga", DeckPage.HomeId, [new DeckKey(2, KeyKinds.Page, "Vor Spiel", PageId: "vor")]),
            new DeckPage("vor", "Vor Spiel", "liga", [new DeckKey(1, KeyKinds.Music, "", MusicId: Break.Id)]),
        ]);
        var board = Build(out _, out _, deck);

        await board.PressAsync(DeckId, 1);
        await board.PressAsync(DeckId, 2);
        Assert.Equal("Vor Spiel", board.Title(DeckId));

        await board.PressAsync(DeckId, 4);
        Assert.Equal("Liga", board.Title(DeckId));

        await board.PressAsync(DeckId, 4);
        Assert.Equal("Startseite", board.Title(DeckId));
    }

    [Fact]
    public async Task A_state_key_shows_the_value_and_switches_it()
    {
        var board = Keys(out _, out var states, new DeckKey(1, KeyKinds.Special, "Automodus", Special: SpecialKeys.AutoBreakMusic));

        Assert.Contains("aus", board.Slots(DeckId)[0].Label);

        await board.PressAsync(DeckId, 1);

        Assert.Equal("an", states.Value(ShowStates.AutoBreakMusic));
        Assert.Contains("an", board.Slots(DeckId)[0].Label);
    }

    [Fact]
    public async Task Panic_switches_the_automatic_music_off_again()
    {
        var board = Keys(out _, out var states, new DeckKey(1, KeyKinds.Special, "Automodus", Special: SpecialKeys.AutoBreakMusic));
        await board.PressAsync(DeckId, 1);

        await board.PanicAsync(CancellationToken.None);

        Assert.Equal("aus", states.Value(ShowStates.AutoBreakMusic));
    }

    [Fact]
    public void The_view_carries_the_grid_the_hall_configured()
    {
        var board = Keys(out _, out _);

        var view = board.View(DeckId);
        var json = System.Text.Json.JsonSerializer.Serialize(view, PayloadJson.Options);

        Assert.Contains("\"columns\":3", json);
        Assert.Contains("\"rows\":2", json);
        Assert.Contains("\"slots\":", json);
    }

    [Fact]
    public void An_answer_without_a_change_leaves_the_slots_out()
    {
        var board = Keys(out _, out _);

        var json = System.Text.Json.JsonSerializer.Serialize(board.View(DeckId, board.Version), PayloadJson.Options);

        Assert.DoesNotContain("\"slots\"", json);
        Assert.Contains("\"live\"", json);
        Assert.Contains("\"states\"", json);
    }

    [Fact]
    public async Task Pressing_a_key_that_does_not_exist_says_so()
    {
        var board = Keys(out _, out _);

        Assert.Equal("Taste 99 gibt es nicht", await board.PressAsync(DeckId, 99));
        Assert.Equal("Dieses Deck gibt es nicht", await board.PressAsync("gibt-es-nicht", 1));
    }

    [Fact]
    public void A_deck_the_hall_does_not_have_shows_nothing()
    {
        var board = Keys(out _, out _);

        Assert.Empty(board.Slots("anderes"));
    }

    [Fact]
    public void Every_change_moves_the_version_on()
    {
        var board = Keys(out _, out var states);
        var before = board.Version;

        states.Set(ShowStates.Led, "läuft", "Test");

        Assert.True(board.Version > before);
    }
}
