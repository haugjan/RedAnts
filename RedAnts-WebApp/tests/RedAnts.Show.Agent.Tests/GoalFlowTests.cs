using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class GoalFlowTests
{
    const string DeckId = "mk2";

    static readonly GameTeam RedAnts = GameTeam.Empty with
    {
        ShortName = "RA",
        Players = [new Player(7, "Anna Habersaat"), new Player(9, "Ida Sundberg"), new Player(12, "Lea Muster"),
            new Player(14, "Mia Beispiel"), new Player(21, "Zoe Test"), new Player(30, "Goalie Eins")],
        Starting6 = [30, 7, 9, 12, 14, 21],
    };

    static readonly GameTeam Guests = GameTeam.Empty with { ShortName = "GEG", Players = [new Player(5, "Gast Fünf")] };

    static Deck.Deck Build(DeckLayout layout, GameTeam home, GameTeam? away = null, int teamId = 0, bool led = true)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with { Id = "win4", Name = "Win4", TeamId = teamId, Decks = [layout], Offered = new HallCapabilities(Led: led) };
        store.Apply(new AgentPayload(hall, Game.None with { Id = "1", Home = home, Away = away ?? Guests }, DateTimeOffset.Now, []));
        var states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var runner = new KeyRunner(store, live, states, new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
        return new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance);
    }

    static DeckLayout Grid(int columns, int rows, params DeckKey[] keys) =>
        new(DeckId, "Streamdeck Mk2", columns, rows, [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)], PanicKey: 11, BackKey: 1);

    static DeckKey Special(int number, string special, string label) => new(number, KeyKinds.Special, label, Special: special);

    static Task PressLabel(Deck.Deck deck, string label) =>
        deck.PressAsync(DeckId, deck.Slots(DeckId).Single(s => s.Label == label).Number);

    static List<string> Labels(Deck.Deck deck) => deck.Slots(DeckId).Select(s => s.Label).ToList();

    [Fact]
    public async Task A_home_goal_asks_scorer_then_assist_and_leaves_the_led_to_the_speaker_key()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalHome, "Tor")), RedAnts);

        await PressLabel(deck, "Tor");
        Assert.Contains("Torschützin?", Labels(deck));

        await PressLabel(deck, "12\nLea Muster");
        Assert.Contains("Assist?", Labels(deck));
        Assert.Contains("Kein Assist", Labels(deck));

        await PressLabel(deck, "7\nAnna H.");
        Assert.Contains("LED Torschützin\n#12 Lea Muster", Labels(deck));

        await PressLabel(deck, "LED Torschützin\n#12 Lea Muster");
        Assert.Contains("LED Assist\n#7 Anna H.", Labels(deck));

        await PressLabel(deck, "LED Assist\n#7 Anna H.");
        await PressLabel(deck, "✓ Fertig");
        Assert.Contains("Tor", Labels(deck));
    }

    [Fact]
    public async Task Without_an_assist_the_speaker_key_goes_from_the_scorer_straight_to_done()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalHome, "Tor")), RedAnts);

        await PressLabel(deck, "Tor");
        await PressLabel(deck, "12\nLea Muster");
        await PressLabel(deck, "Kein Assist");
        await PressLabel(deck, "LED Torschützin\n#12 Lea Muster");

        Assert.Contains("✓ Fertig", Labels(deck));
    }

    [Fact]
    public async Task A_hall_without_led_ends_the_home_goal_after_the_scorer()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalHome, "Tor")), RedAnts, led: false);

        await PressLabel(deck, "Tor");
        await PressLabel(deck, "12\nLea Muster");

        Assert.Contains("Tor", Labels(deck));
        Assert.DoesNotContain("Assist?", Labels(deck));
    }

    [Fact]
    public async Task A_guest_goal_ends_after_the_scorer()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalAway, "Tor Gast")), RedAnts);

        await PressLabel(deck, "Tor Gast");
        await PressLabel(deck, "5\nGast Fünf");

        Assert.Contains("Tor Gast", Labels(deck));
        Assert.DoesNotContain("Assist?", Labels(deck));
    }

    [Fact]
    public async Task When_the_red_ants_play_away_their_goal_gets_the_assist_and_speaker_steps()
    {
        var home = Guests with { TeamId = 1 };
        var away = RedAnts with { TeamId = 428665 };
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalAway, "Tor RA")), home, away, teamId: 428665);

        await PressLabel(deck, "Tor RA");
        await PressLabel(deck, "12\nLea Muster");

        Assert.Contains("Assist?", Labels(deck));
    }

    [Fact]
    public async Task A_small_deck_with_a_big_roster_asks_for_the_jersey_number()
    {
        var roster = RedAnts with { Players = Enumerable.Range(1, 20).Select(n => new Player(n, $"Spielerin {n}")).ToList() };
        var deck = Build(Grid(5, 3, Special(2, SpecialKeys.GoalHome, "Tor")), roster);

        await PressLabel(deck, "Tor");
        Assert.Contains("Torschützin?\nNummer tippen", Labels(deck));
        Assert.Contains("Eigentor", Labels(deck));

        await PressLabel(deck, "1");
        await PressLabel(deck, "2");
        Assert.Contains("#12 ✓\nSpielerin 12", Labels(deck));

        await PressLabel(deck, "⌫");
        await PressLabel(deck, "⌫");
        await PressLabel(deck, "9");
        await PressLabel(deck, "#9 ✓\nSpielerin 9");

        Assert.Contains("Assist?\nNummer tippen", Labels(deck));
        Assert.Contains("Kein Assist", Labels(deck));
    }

    [Fact]
    public async Task An_unknown_number_cannot_be_confirmed()
    {
        var roster = RedAnts with { Players = Enumerable.Range(1, 20).Select(n => new Player(n, $"Spielerin {n}")).ToList() };
        var deck = Build(Grid(5, 3, Special(2, SpecialKeys.GoalHome, "Tor")), roster);

        await PressLabel(deck, "Tor");
        await PressLabel(deck, "9");
        await PressLabel(deck, "9");

        var display = deck.Slots(DeckId).Single(s => s.Label == "#99\n?");
        Assert.False(display.Pressable);
    }

    [Fact]
    public void Starting_six_without_a_lineup_says_where_to_enter_it()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.Starting6Home, "Starting 6")), RedAnts with { Starting6 = [] });

        var slot = deck.Slots(DeckId).Single(s => s.Number == 2);

        Assert.Equal("Starting 6 fehlt\n⚙ Spiel", slot.Label);
        Assert.False(slot.Pressable);
    }

    [Fact]
    public async Task Starting_six_of_the_guests_opens_no_lineup()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.Starting6Away, "Starting 6 Gast")), RedAnts);

        await PressLabel(deck, "Starting 6 Gast");

        Assert.Contains("Starting 6 Gast", Labels(deck));
    }

    [Fact]
    public async Task Starting_six_shows_the_pressed_player_and_next_walks_on()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.Starting6Home, "Starting 6")), RedAnts);

        await PressLabel(deck, "Starting 6");
        await PressLabel(deck, "9\nIda Sundberg");
        Assert.Equal(0xFFD400, deck.Slots(DeckId).Single(s => s.Label == "9\nIda Sundberg").Color);

        await PressLabel(deck, "▶▶ Weiter");
        Assert.Equal(0xFFD400, deck.Slots(DeckId).Single(s => s.Label == "12\nLea Muster").Color);
    }
}
