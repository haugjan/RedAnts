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
    public async Task A_home_goal_asks_the_scorer_and_is_then_done()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalHome, "Tor")), RedAnts);

        await PressLabel(deck, "Tor");
        Assert.Contains("Torschützin?", Labels(deck));

        await PressLabel(deck, "12\nLea Muster");

        Assert.Contains("Tor", Labels(deck));
        Assert.DoesNotContain("Assist?", Labels(deck));
    }

    [Fact]
    public async Task The_led_page_offers_the_four_occasions()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)")), RedAnts);

        await PressLabel(deck, "Spieler (LED)");

        Assert.Contains("Tor", Labels(deck));
        Assert.Contains("Assist", Labels(deck));
        Assert.Contains("Strafe", Labels(deck));
        Assert.Contains("Opening", Labels(deck));
    }

    [Fact]
    public async Task After_a_player_the_led_page_comes_back_for_the_next_one()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)")), RedAnts);

        await PressLabel(deck, "Spieler (LED)");
        await PressLabel(deck, "Tor");
        Assert.Contains("12\nLea Muster", Labels(deck));

        await PressLabel(deck, "12\nLea Muster");

        Assert.Contains("Assist", Labels(deck));
        Assert.Contains("Opening", Labels(deck));
    }

    [Fact]
    public async Task The_opening_stays_on_the_list_for_the_next_player()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)")), RedAnts);

        await PressLabel(deck, "Spieler (LED)");
        await PressLabel(deck, "Opening");
        await PressLabel(deck, "12\nLea Muster");

        Assert.Contains("7\nAnna H.", Labels(deck));
        Assert.DoesNotContain("Opening", Labels(deck));
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
    public async Task When_the_red_ants_play_away_their_goal_also_ends_after_the_scorer()
    {
        var home = Guests with { TeamId = 1 };
        var away = RedAnts with { TeamId = 428665 };
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalAway, "Tor RA")), home, away, teamId: 428665);

        await PressLabel(deck, "Tor RA");
        await PressLabel(deck, "12\nLea Muster");

        Assert.Contains("Tor RA", Labels(deck));
        Assert.DoesNotContain("Assist?", Labels(deck));
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

        Assert.Contains("Tor", Labels(deck));
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
    public async Task A_missing_vmix_does_not_keep_the_led_page_off_the_wall()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with
        {
            Id = "win4",
            Decks = [Grid(8, 4, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)"))],
            Offered = new HallCapabilities(),
            Devices = new HallDevices(VmixHost: "127.0.0.1", VmixPort: 1, SednaHost: "127.0.0.1", SednaPort: port),
            LedTemplates = [new LedTemplate("spielerin", "start playlist -n \"Spielerin {team.short} {player.number}\" -o")],
            OverlayTitles = [new OverlayTitle("penalty", "Spielerin", 1, new Dictionary<string, string>())],
        };
        store.Apply(new AgentPayload(hall, Game.None with { Id = "1", Home = RedAnts, Away = Guests }, DateTimeOffset.Now, []));
        var states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var runner = new KeyRunner(store, live, states, new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
        var deck = new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await PressLabel(deck, "Spieler (LED)");
        await PressLabel(deck, "Strafe");
        var heard = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync(stop.Token);
            var buffer = new byte[256];
            var read = await client.GetStream().ReadAsync(buffer, stop.Token);
            return System.Text.Encoding.UTF8.GetString(buffer, 0, read).Trim();
        });
        var note = await deck.PressAsync(DeckId, deck.Slots(DeckId).Single(s => s.Label == "12\nLea Muster").Number);

        Assert.Null(note);
        Assert.Equal("start playlist -n \"Spielerin RA 12\" -o", await heard);
        Assert.Null(deck.RecentFault);
        listener.Stop();
    }

    [Fact]
    public async Task The_question_sits_next_to_back()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalHome, "Tor")), RedAnts);

        await PressLabel(deck, "Tor");
        var slots = deck.Slots(DeckId);

        Assert.Equal("◀ Zurück", slots.Single(s => s.Number == 1).Label);
        Assert.Equal("Torschützin?", slots.Single(s => s.Number == 2).Label);
        Assert.True(slots.Single(s => s.Label == "12\nLea Muster").Pressable);
    }

    [Fact]
    public async Task On_the_xl_the_goal_pages_put_back_top_left_even_though_back_is_on_17()
    {
        var layout = new DeckLayout(DeckId, "Streamdeck XL", 8, 4,
            [new DeckPage(DeckPage.HomeId, "Startseite", null, [Special(1, SpecialKeys.GoalHome, "Tor")])], PanicKey: 25, BackKey: 17);
        var deck = Build(layout, RedAnts);

        await PressLabel(deck, "Tor");
        var slots = deck.Slots(DeckId);

        Assert.Equal("◀ Zurück", slots.Single(s => s.Number == 1).Label);
        Assert.Equal("Torschützin?", slots.Single(s => s.Number == 2).Label);
        Assert.Equal("🚨 PANIC", slots.Single(s => s.Number == 25).Label);
    }

    [Fact]
    public async Task The_led_page_says_which_team_it_shows()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.PlayerHome, "Spieler (LED)")), RedAnts);

        await PressLabel(deck, "Spieler (LED)");
        Assert.Equal("Auf LED zeigen\nRA", deck.Slots(DeckId).Single(s => s.Number == 2).Label);

        await PressLabel(deck, "Strafe");
        Assert.Equal("Strafe RA\nauf LED", deck.Slots(DeckId).Single(s => s.Number == 2).Label);
    }

    [Fact]
    public async Task A_goal_key_without_led_skips_assist_and_speaker()
    {
        var key = Special(2, SpecialKeys.GoalHome, "Tor") with { Led = false };
        var deck = Build(Grid(8, 4, key), RedAnts);

        await PressLabel(deck, "Tor");
        await PressLabel(deck, "12\nLea Muster");

        Assert.DoesNotContain("Assist?", Labels(deck));
        Assert.Contains("Tor", Labels(deck));
    }

    [Fact]
    public async Task A_player_key_without_any_output_only_runs_its_commands()
    {
        var key = Special(2, SpecialKeys.PlayerHome, "Spielerin") with { Led = false, Livestream = false };
        var deck = Build(Grid(8, 4, key), RedAnts);

        Assert.False(deck.Slots(DeckId).Single(s => s.Number == 2).Pressable);

        var withSound = Build(Grid(8, 4, key with { Steps = [new KeyStep(KeyActions.MusicPlay, "x")] }), RedAnts);
        await PressLabel(withSound, "Spielerin");
        Assert.Contains("Spielerin", Labels(withSound));
        Assert.DoesNotContain("Spielerin Heim RA", Labels(withSound));
    }

    [Fact]
    public void A_led_only_player_key_works_in_a_hall_without_vmix()
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var key = Special(2, SpecialKeys.PlayerHome, "Spielerin") with { Livestream = false };
        var hall = Hall.Offline with { Id = "win4", Decks = [Grid(8, 4, key)], Offered = new HallCapabilities(Vmix: false, Led: true) };
        store.Apply(new AgentPayload(hall, Game.None with { Id = "1", Home = RedAnts, Away = Guests }, DateTimeOffset.Now, []));
        var states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var runner = new KeyRunner(store, live, states, new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
        var deck = new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance);

        Assert.True(deck.Slots(DeckId).Single(s => s.Number == 2).Pressable);
    }

    [Fact]
    public void A_stored_key_without_the_new_switches_shows_on_livestream_and_led()
    {
        var key = System.Text.Json.JsonSerializer.Deserialize<DeckKey>("""{ "number": 2, "kind": "spezial", "special": "tor-heim" }""", PayloadJson.Options)!;

        Assert.True(key.Livestream);
        Assert.True(key.Led);
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
    public async Task The_top_scorer_wears_her_title_on_the_key_instead_of_her_number()
    {
        var deck = Build(Grid(8, 4, Special(2, SpecialKeys.GoalHome, "Tor")), RedAnts with { TopScorer = 12 });

        await PressLabel(deck, "Tor");
        var labels = Labels(deck);

        Assert.Contains($"{Game.TopScorerLabel}\nLea Muster", labels);
        Assert.DoesNotContain("12\nLea Muster", labels);
        Assert.Contains("7\nAnna H.", labels);
        Assert.Single(labels, label => label.StartsWith(Game.TopScorerLabel));
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
