using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Scenes;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class ConfiguredDeckTests
{
    private static readonly IReadOnlyList<Scene> Scenes =
    [
        new("tor-heim", "Tor Heim", "home", [new(SceneActions.OverlayShow, "title:player_event")]),
        new("sys-ausblenden", "Ausblenden", "", [new(SceneActions.OverlayHide, "title:*")]),
        new("sys-einblenden", "Letzten Einblender zeigen", "", [new(SceneActions.OverlayShow, "title:result")]),
        new("overlay-aus", "Einblender ausschalten", "", [new(SceneActions.StateSet, ShowStates.Overlay, Event: "aus")]),
    ];

    private static readonly IReadOnlyList<ShowState> States =
    [
        new(ShowStates.Overlay, "Einblender", ["aus", "an"], "beobachtet"),
    ];

    private static readonly IReadOnlyList<SoundCommand> Sounds =
    [
        new("tor", "Tor-Jingle", "#ff7c80", null, [new SoundTrack(SoundSource.Blob, "tor.mp3")]),
    ];

    private static DeckLayout Layout() => new(DeckLayout.FieldDeck, "XL", 8, 4,
    [
        new DeckKey(1, DeckKeyKinds.Scene, "Tor", "#ff7c80", SceneId: "tor-heim"),
        new DeckKey(2, DeckKeyKinds.Sound, "Jingle", "#2b3038", SoundId: "tor"),
        new DeckKey(3, DeckKeyKinds.Page, "Mitteilung", "#c1f0c8", Page: "Message"),
        new DeckKey(4, DeckKeyKinds.Live, "Stand", "#ff7c80", Live: "score.home"),
        new DeckKey(5, DeckKeyKinds.Scene, "Fehlt", "#2b3038", SceneId: "gibt-es-nicht"),
        new DeckKey(17, DeckKeyKinds.Back, "◀ Zurück", "#8ed973"),
        new DeckKey(25, DeckKeyKinds.Scene, "🚨 PANIC", "#ff0000", SceneId: "sys-ausblenden"),
        new DeckKey(32, DeckKeyKinds.State, "Einblender", "#1e7b34", StateId: ShowStates.Overlay, Rows:
        [
            new DeckStateRow("aus", "Einblender aus", "einblender-aus", "#1e7b34", DeckStateRow.RunScene, "sys-einblenden"),
            new DeckStateRow("an", "Einblender an", "einblender-ein", "#c0201c", DeckStateRow.RunScene, "overlay-aus"),
        ]),
    ]);

    private static (Deck.Deck Deck, ShowStates States, PayloadStore Store) Build()
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with { Id = "win4", Decks = [Layout()] };
        store.Apply(new AgentPayload(hall, Scenes, null, DateTimeOffset.Now, Sounds, States));
        var live = new LiveGame();
        var vmix = new VmixLink(store, live, NullLogger<VmixLink>.Instance);
        var led = new SednaLink(store, NullLogger<SednaLink>.Instance);
        var states = new ShowStates(store, NullLogger<ShowStates>.Instance);
        var runner = new SceneRunner(store, live, states, vmix, led, NullLogger<SceneRunner>.Instance);
        var deck = new Deck.Deck(store, live, states, runner, vmix, led, NullLogger<Deck.Deck>.Instance);
        return (deck, states, store);
    }

    private static JsonElement Slot(Deck.Deck deck, int number) =>
        JsonSerializer.SerializeToElement(deck.View()).GetProperty("slots").EnumerateArray()
            .Single(s => s.GetProperty("number").GetInt32() == number);

    [Fact]
    public void The_start_page_follows_the_layout_from_the_cloud()
    {
        var (deck, _, _) = Build();

        Assert.Equal("Tor", Slot(deck, 1).GetProperty("label").GetString());
        Assert.Equal(0xff7c80, Slot(deck, 1).GetProperty("bgcolor").GetInt32());
        Assert.Equal("Jingle", Slot(deck, 2).GetProperty("label").GetString());
        Assert.Equal("Mitteilung", Slot(deck, 3).GetProperty("label").GetString());
        Assert.True(Slot(deck, 3).GetProperty("pressable").GetBoolean());
        Assert.Equal("◀ Zurück", Slot(deck, 17).GetProperty("label").GetString());
        Assert.Equal("", Slot(deck, 8).GetProperty("label").GetString());
    }

    [Fact]
    public void A_live_key_shows_the_score_and_opens_the_correction()
    {
        var (deck, _, _) = Build();

        Assert.Equal("0", Slot(deck, 4).GetProperty("label").GetString());
        Assert.True(Slot(deck, 4).GetProperty("pressable").GetBoolean());
    }

    [Fact]
    public void A_key_whose_scene_is_gone_stays_dark_and_dead()
    {
        var (deck, _, _) = Build();

        Assert.False(Slot(deck, 5).GetProperty("pressable").GetBoolean());
    }

    [Fact]
    public void A_state_key_follows_the_state()
    {
        var (deck, states, _) = Build();

        Assert.Equal("Einblender aus", Slot(deck, 32).GetProperty("label").GetString());
        Assert.Equal(0x1e7b34, Slot(deck, 32).GetProperty("bgcolor").GetInt32());

        states.Set(ShowStates.Overlay, "an", "Test");

        Assert.Equal("Einblender an", Slot(deck, 32).GetProperty("label").GetString());
        Assert.Equal(0xc0201c, Slot(deck, 32).GetProperty("bgcolor").GetInt32());
    }

    [Fact]
    public async Task A_music_key_reports_the_sound_to_the_cloud()
    {
        var (deck, _, _) = Build();
        SoundCommand? requested = null;
        deck.SoundRequested += sound => requested = sound;

        await deck.PressAsync(2);

        Assert.Equal("tor", requested?.Id);
    }

    [Fact]
    public async Task Pressing_a_state_key_runs_the_scene_of_its_row()
    {
        var (deck, states, _) = Build();
        states.Set(ShowStates.Overlay, "an", "Test");

        await deck.PressAsync(32);
        for (var i = 0; i < 100 && states.Value(ShowStates.Overlay) != "aus"; i++) await Task.Delay(10);

        Assert.Equal("aus", states.Value(ShowStates.Overlay));
    }

    [Fact]
    public void Without_a_layout_the_built_in_deck_is_used()
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        store.Apply(new AgentPayload(Hall.Offline with { Id = "win4" }, Scenes, null, DateTimeOffset.Now, Sounds, States));
        var live = new LiveGame();
        var vmix = new VmixLink(store, live, NullLogger<VmixLink>.Instance);
        var led = new SednaLink(store, NullLogger<SednaLink>.Instance);
        var states = new ShowStates(store, NullLogger<ShowStates>.Instance);
        var runner = new SceneRunner(store, live, states, vmix, led, NullLogger<SceneRunner>.Instance);
        var deck = new Deck.Deck(store, live, states, runner, vmix, led, NullLogger<Deck.Deck>.Instance);

        Assert.Equal("Tor", Slot(deck, 1).GetProperty("label").GetString());
        Assert.Equal("Coach", Slot(deck, 2).GetProperty("label").GetString());
    }
}
