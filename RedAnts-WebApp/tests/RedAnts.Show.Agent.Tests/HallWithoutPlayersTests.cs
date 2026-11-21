using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class HallWithoutPlayersTests
{
    const string DeckId = "xl";

    static readonly GameTeam RedAnts = GameTeam.Empty with
    {
        ShortName = "RA",
        Players = [new Player(7, "Anna Habersaat"), new Player(12, "Lea Muster")],
        Starting6 = [7, 12],
    };

    static DeckKey Special(int number, string special, string label, params KeyStep[] steps) =>
        new(number, KeyKinds.Special, label, Special: special, Steps: steps);

    static Deck.Deck Build(bool players, params DeckKey[] keys)
    {
        var layout = new DeckLayout(DeckId, "Streamdeck XL", 8, 4,
            [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)], PanicKey: 25, BackKey: 17);
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with
        {
            Id = "win4-sound-led",
            Decks = [layout],
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: false, Led: true, Players: players),
            LedTemplates = [new LedTemplate("tor", "start playlist -n \"STR_RA_TOR\" -o")],
        };
        store.Apply(new AgentPayload(hall, Game.None with { Id = "1", Home = RedAnts }, DateTimeOffset.Now, []));
        var states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var runner = new KeyRunner(store, live, states, new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
        return new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance);
    }

    static List<string> Labels(Deck.Deck deck) => deck.Slots(DeckId).Select(s => s.Label).ToList();

    static readonly KeyStep LedTor = new(KeyActions.LedStart, "tor");

    [Fact]
    public async Task A_goal_plays_its_sequence_and_asks_for_nobody()
    {
        var deck = Build(false, Special(2, SpecialKeys.GoalHome, "Tor", LedTor));

        await deck.PressAsync(DeckId, 2);

        Assert.DoesNotContain("Torschützin?", Labels(deck));
        Assert.Contains("Tor", Labels(deck));
    }

    [Fact]
    public async Task With_players_the_same_key_opens_the_list()
    {
        var deck = Build(true, Special(2, SpecialKeys.GoalHome, "Tor", LedTor));

        await deck.PressAsync(DeckId, 2);

        Assert.Contains("Torschützin?", Labels(deck));
    }

    [Fact]
    public void A_key_that_only_picks_a_player_disappears()
    {
        var without = Build(false, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)"));
        var with = Build(true, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)"));

        Assert.DoesNotContain("Spieler (LED)", Labels(without));
        Assert.Contains("Spieler (LED)", Labels(with));
    }

    [Fact]
    public void A_penalty_key_with_music_and_led_stays()
    {
        var deck = Build(false, Special(2, SpecialKeys.PenaltyHome, "Strafe", LedTor));

        Assert.Contains("Strafe", Labels(deck));
    }

    [Fact]
    public void A_hall_offers_players_unless_it_says_otherwise() =>
        Assert.True(HallCapabilities.Default.Players);

    [Fact]
    public void The_default_screen_falls_back_when_none_is_chosen()
    {
        Assert.Equal(Hall.FallbackScreen, Hall.Offline.Screen);
        Assert.Equal("spiel-u21", (Hall.Offline with { DefaultScreen = "spiel-u21" }).Screen);
    }
}
