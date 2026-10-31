using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RedAnts.Show.Agent.Cloud;
using RedAnts.Show.Agent.Keys;
using RedAnts.Show.Agent.Led;
using RedAnts.Show.Agent.Vmix;

namespace RedAnts.Show.Agent.Tests;

public class GuestLogoTests
{
    const string DeckId = "xl";

    static readonly GameTeam RedAnts = GameTeam.Empty with
    {
        TeamId = 428665,
        LongName = "Red Ants Winterthur",
        ShortName = "RA",
        Players = [new Player(7, "Anna Habersaat"), new Player(9, "Ida Sundberg"), new Player(12, "Lea Muster"),
            new Player(14, "Mia Beispiel"), new Player(21, "Zoe Test"), new Player(30, "Goalie Eins")],
        Starting6 = [30, 7, 9, 12, 14, 21],
    };

    static GameTeam Guest(int? teamId, string name) =>
        GameTeam.Empty with { TeamId = teamId, LongName = name, ShortName = GameTeam.Empty.ShortName };

    [Theory]
    [InlineData(430340, "gast-bern")]
    [InlineData(432516, "gast-chur")]
    [InlineData(431166, "gast-jets")]
    [InlineData(427952, "gast-laupen")]
    [InlineData(428990, "gast-riders")]
    [InlineData(427766, "gast-skorps")]
    [InlineData(429324, "gast-uri")]
    [InlineData(430107, "gast-wizards")]
    [InlineData(429578, "gast-zug")]
    public void Every_league_team_is_found_by_its_swissunihockey_id(int teamId, string led) =>
        Assert.Equal(led, GuestTeams.EventOf(Guest(teamId, "Umbenannt")));

    [Theory]
    [InlineData("Unihockey Berner Oberland", "gast-bern")]
    [InlineData("Floorball Chur United", "gast-chur")]
    [InlineData("Kloten-Dietlikon Jets", "gast-jets")]
    [InlineData("UHC Laupen ZH", "gast-laupen")]
    [InlineData("FB Riders DBR", "gast-riders")]
    [InlineData("UHV Skorpion Emmental", "gast-skorps")]
    [InlineData("Floorball Uri", "gast-uri")]
    [InlineData("Wizards Bern Burgdorf", "gast-wizards")]
    [InlineData("Zug United", "gast-zug")]
    public void A_team_without_an_id_is_found_by_its_name(string name, string led) =>
        Assert.Equal(led, GuestTeams.EventOf(Guest(null, name)));

    [Fact]
    public void The_two_bernese_teams_do_not_get_mixed_up()
    {
        Assert.Equal("gast-wizards", GuestTeams.EventOf(Guest(null, "Wizards Bern Burgdorf")));
        Assert.Equal("gast-bern", GuestTeams.EventOf(Guest(null, "Unihockey Berner Oberland")));
    }

    [Theory]
    [InlineData("Unihockey Gurmels")]
    [InlineData("piranha chur")]
    [InlineData("Waldkirch-St. Gallen")]
    [InlineData("")]
    public void A_team_outside_the_group_has_no_playlist(string name) =>
        Assert.Null(GuestTeams.EventOf(Guest(null, name)));

    static DeckLayout Grid() =>
        new(DeckId, "Streamdeck XL", 8, 4,
            [new DeckPage(DeckPage.HomeId, "Startseite", null,
            [
                new DeckKey(2, KeyKinds.Special, "Starting 6 Gast", Special: SpecialKeys.Starting6Away),
                new DeckKey(3, KeyKinds.Special, "Starting 6", Special: SpecialKeys.Starting6Home),
                new DeckKey(4, KeyKinds.Special, "Logo Gast", Special: SpecialKeys.GuestLogo),
            ])], PanicKey: 25, BackKey: 17);

    static Deck.Deck Build(int port, GameTeam away, bool led = true)
    {
        var store = new PayloadStore(NullLogger<PayloadStore>.Instance);
        var hall = Hall.Offline with
        {
            Id = "win4",
            TeamId = RedAnts.TeamId ?? 0,
            Decks = [Grid()],
            Offered = new HallCapabilities(Led: led),
            Devices = new HallDevices(SednaHost: "127.0.0.1", SednaPort: port),
            LedTemplates = [new LedTemplate("gast-chur", "start playlist -n \"STR_RA_Chur\"")],
        };
        store.Apply(new AgentPayload(hall, Game.None with { Id = "1", Home = RedAnts, Away = away }, DateTimeOffset.Now, []));
        var states = new ShowStates(NullLogger<ShowStates>.Instance);
        var live = new LiveGame();
        var runner = new KeyRunner(store, live, states, new VmixLink(store, live, NullLogger<VmixLink>.Instance),
            new SednaLink(store, NullLogger<SednaLink>.Instance), NullLogger<KeyRunner>.Instance);
        return new Deck.Deck(store, live, states, runner, NullLogger<Deck.Deck>.Instance);
    }

    static async Task<string> HeardAsync(TcpListener listener, CancellationToken stop)
    {
        using var client = await listener.AcceptTcpClientAsync(stop);
        var buffer = new byte[256];
        var read = await client.GetStream().ReadAsync(buffer, stop);
        return Encoding.UTF8.GetString(buffer, 0, read).Trim();
    }

    static Task Press(Deck.Deck deck, string label) =>
        deck.PressAsync(DeckId, deck.Slots(DeckId).Single(slot => slot.Label == label).Number);

    [Fact]
    public async Task Starting_six_of_the_guests_puts_their_logo_on_the_led_wall()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var heard = HeardAsync(listener, stop.Token);

        var deck = Build(port, Guest(432516, "Floorball Chur United"));
        await Press(deck, "Starting 6 Gast");

        Assert.Equal("start playlist -n \"STR_RA_Chur\"", await heard);
        listener.Stop();
    }

    [Fact]
    public async Task Starting_six_of_the_home_team_sends_no_logo_and_opens_the_lineup()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var deck = Build(port, Guest(432516, "Floorball Chur United"));
        await Press(deck, "Starting 6");

        Assert.False(listener.Pending());
        Assert.Contains("30\nGoalie Eins", deck.Slots(DeckId).Select(slot => slot.Label));
        listener.Stop();
    }

    [Fact]
    public async Task A_guest_without_a_template_leaves_the_led_wall_alone()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var deck = Build(port, Guest(null, "Unihockey Gurmels"));
        await Press(deck, "Starting 6 Gast");

        Assert.False(listener.Pending());
        listener.Stop();
    }

    [Fact]
    public void The_guest_key_wears_the_logo_and_the_short_name_of_the_loaded_game()
    {
        var deck = Build(0, Guest(432516, "Floorball Chur United") with { ShortName = "FCU" });

        var slot = deck.Slots(DeckId).Single(s => s.Number == 4);

        Assert.Equal("FCU", slot.Label);
        Assert.StartsWith("team-chur|", slot.ImageKey);
        Assert.True(slot.Pressable);
    }

    [Fact]
    public void The_guest_key_changes_with_the_game()
    {
        Assert.StartsWith("team-wizards|", Build(0, Guest(430107, "Wizards Bern Burgdorf")).Slots(DeckId).Single(s => s.Number == 4).ImageKey);
        Assert.StartsWith("team-zug|", Build(0, Guest(429578, "Zug United")).Slots(DeckId).Single(s => s.Number == 4).ImageKey);
    }

    [Fact]
    public void Without_a_known_guest_the_key_points_at_the_game_settings()
    {
        var slot = Build(0, Guest(null, "Unihockey Gurmels")).Slots(DeckId).Single(s => s.Number == 4);

        Assert.Equal("Logo Gast\n⚙ Spiel", slot.Label);
        Assert.False(slot.Pressable);
    }

    [Fact]
    public async Task The_guest_key_puts_that_logo_on_the_led_wall()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var heard = HeardAsync(listener, stop.Token);

        var deck = Build(port, Guest(432516, "Floorball Chur United") with { ShortName = "FCU" });
        await deck.PressAsync(DeckId, 4);

        Assert.Equal("start playlist -n \"STR_RA_Chur\"", await heard);
        listener.Stop();
    }

    [Fact]
    public async Task A_hall_without_a_led_wall_sends_nothing()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var deck = Build(port, Guest(432516, "Floorball Chur United"), led: false);
        await Press(deck, "Starting 6 Gast");

        Assert.False(listener.Pending());
        listener.Stop();
    }
}
