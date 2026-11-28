using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Deck;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class TabBoardTests
{
    const string DeckId = "xl";

    static DeckLayout Page(string label, params DeckKey[] keys) =>
        new(DeckId, "XL", 4, 2, [new DeckPage(DeckPage.HomeId, label, null, keys)]);

    static readonly DeckLayout Classic = Page("Startseite", new DeckKey(1, KeyKinds.Music, "Alt", MusicId: "alt"));

    static TabDeckVariant Variant(string league, string tab, string label) =>
        new(DeckId, league, tab, Page(tab, new DeckKey(1, KeyKinds.Music, label, MusicId: label)));

    static readonly TabBoardPayload Board = new(
        [new TabChoice("lupl", "L-UPL"), new TabChoice("u21", "U21")],
        [new TabChoice("vor-spiel", "Vor dem Spiel"), new TabChoice("spiel", "Während dem Spiel")],
        "lupl",
        [
            Variant("lupl", "vor-spiel", "Mobiliar"),
            Variant("lupl", "spiel", "Tor"),
            Variant("u21", "vor-spiel", "Wartesong"),
            Variant("u21", "spiel", "Tor U21"),
        ]);

    static (Deck.Deck Deck, TabSession Tabs) Build(TabBoardPayload? board = null)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with { Id = "win4", Name = "Win4", Decks = [Classic] };
        store.Apply(new AgentPayload(hall, Game.None, DateTimeOffset.Now, [], board ?? Board));
        var tabs = new TabSession(store, null);
        var states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var runner = new KeyRunner(store, live, states, new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
        return (new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance, tabs), tabs);
    }

    static string FirstLabel(Deck.Deck deck) => deck.Slots(DeckId).First(s => s.Number == 1).Label;

    [Fact]
    public void Until_the_board_is_started_the_deck_stays_as_it_was()
    {
        var (deck, _) = Build();

        Assert.Equal("Alt", FirstLabel(deck));
    }

    [Fact]
    public void Started_the_deck_shows_the_first_tab_of_the_league()
    {
        var (deck, tabs) = Build();

        tabs.Start("u21");

        Assert.Equal("Wartesong", FirstLabel(deck));
    }

    [Fact]
    public void A_tab_and_a_league_switch_the_deck()
    {
        var (deck, tabs) = Build();
        tabs.Start("lupl");

        tabs.Choose("spiel");
        Assert.Equal("Tor", FirstLabel(deck));

        tabs.ChooseLeague("u21");
        Assert.Equal("Tor U21", FirstLabel(deck));
    }

    [Fact]
    public void Stopping_brings_the_previous_deck_back()
    {
        var (deck, tabs) = Build();
        tabs.Start("lupl");

        tabs.Stop();

        Assert.Equal("Alt", FirstLabel(deck));
    }

    [Fact]
    public void A_cloud_without_tabs_leaves_the_deck_alone()
    {
        var (deck, tabs) = Build(Board with { Variants = [] });

        tabs.Start("lupl");

        Assert.False(tabs.Active);
        Assert.Equal("Alt", FirstLabel(deck));
    }

    [Fact]
    public void An_unknown_league_falls_back_to_the_one_of_the_game()
    {
        var (_, tabs) = Build();

        tabs.Start("cup");

        Assert.Equal("lupl", tabs.League);
    }
}
