using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests.TabBoards;

public class TabBoardStartTests
{
    private static readonly Dictionary<string, Hall> NoHalls = new();

    private static ShowSetup Setup(params DeckKey[] keys) => new([], [],
    [
        new Deck("xl", "Streamdeck XL", 8, 4, [new DeckPage(DeckPage.HomeId, "Start", null, keys)], PanicKey: 25, BackKey: 17),
        new Deck("mk2", "Streamdeck Mk2", 5, 3, [new DeckPage(DeckPage.HomeId, "Start", null, [])], PanicKey: 15, BackKey: 11),
    ]);

    [Fact]
    public void The_start_knows_three_halls_and_four_leagues()
    {
        var board = TabBoardStart.Build(Setup(), NoHalls);

        Assert.Equal(["win4", "oberseen", "rennweg"], board.Venues.Select(v => v.Id));
        Assert.Equal(["L-UPL", "U21", "U17", "1. Liga"], board.Leagues.Select(l => l.Name));
    }

    [Fact]
    public void Only_lupl_streams_and_names_players_by_default()
    {
        var leagues = TabBoardStart.Build(Setup(), NoHalls).Leagues;

        Assert.True(leagues.Single(l => l.Id == TabBoardStart.Lupl) is { Livestream: true, Players: true });
        Assert.All(leagues.Where(l => l.Id != TabBoardStart.Lupl), l => Assert.False(l.Livestream || l.Players));
        Assert.Equal("spiel-u21", leagues.Single(l => l.Id == TabBoardStart.U21).ScreenOrDefault);
    }

    [Fact]
    public void A_hall_brings_its_devices_into_the_venue()
    {
        var win4 = Hall.Create("win4", "WIN4 Alles") with { Devices = new HallDevices(SednaHost: "sedna.local") };

        var venue = TabBoardStart.Build(Setup(), new Dictionary<string, Hall> { ["win4"] = win4 }).Venues.Single(v => v.Id == "win4");

        Assert.Equal("sedna.local", venue.Devices.SednaHost);
    }

    [Fact]
    public void A_venue_without_a_hall_offers_sound_only()
    {
        var rennweg = TabBoardStart.Build(Setup(), NoHalls).Venues.Single(v => v.Id == "rennweg");

        Assert.True(rennweg.Equipment is { Sound: true, Vmix: false, Led: false });
    }

    [Fact]
    public void Every_deck_key_becomes_one_button_and_twins_collapse()
    {
        var goal = new DeckKey(1, KeyKinds.Special, "Tor", Special: SpecialKey.GoalHome);
        var music = new DeckKey(2, KeyKinds.Music, "Einlauf", MusicId: "makina");
        var board = TabBoardStart.Build(Setup(goal, music, goal with { Number = 3 }, new DeckKey(4, KeyKinds.Page, "Seite", PageId: "x")), NoHalls);

        Assert.Contains(board.Buttons, b => b.Label == "Tor" && b.Action.Special == SpecialKey.GoalHome);
        Assert.Contains(board.Buttons, b => b.Label == "Einlauf" && b.Action.MusicId == "makina");
        Assert.Single(board.Buttons, b => b.Label == "Tor");
        Assert.DoesNotContain(board.Buttons, b => b.Label == "Seite");
    }

    [Fact]
    public void Buttons_with_the_same_text_get_distinct_ids()
    {
        var board = TabBoardStart.Build(Setup(
            new DeckKey(1, KeyKinds.Music, "Tor", MusicId: "a"),
            new DeckKey(2, KeyKinds.Music, "Tor", MusicId: "b")), NoHalls);

        Assert.Equal(["tor", "tor-2"], board.Buttons.Where(b => b.Label == "Tor").Select(b => b.Id));
    }

    [Fact]
    public void The_xl_follows_the_tab_with_the_music_controls_on_every_tab()
    {
        var board = TabBoardStart.Build(Setup(), NoHalls);
        var xl = board.Deck(TabBoardStart.XlDeck)!;

        Assert.True(xl.FollowsTab);
        Assert.Equal([29, 30, 31, 32], xl.Fixed!.Placements.Select(p => p.Number));
        Assert.All(xl.Fixed.Placements, p => Assert.NotNull(board.Button(p.Button)));
    }

    [Fact]
    public void The_mk2_is_a_deck_of_its_own_page()
    {
        var mk2 = TabBoardStart.Build(Setup(), NoHalls).Deck(TabBoardStart.Mk2Deck)!;

        Assert.False(mk2.FollowsTab);
        Assert.Equal((5, 3, 15, 11), (mk2.Columns, mk2.Rows, mk2.Panic, mk2.Back));
    }

    [Fact]
    public void The_start_is_a_valid_board() => TabBoardStart.Build(Setup(
        new DeckKey(1, KeyKinds.Special, "Tor", Special: SpecialKey.GoalHome),
        new DeckKey(2, KeyKinds.Music, "Einlauf", MusicId: "makina")), NoHalls).Validated();

    [Fact]
    public void A_label_only_key_becomes_a_button() =>
        Assert.Contains(TabBoardStart.Build(Setup(new DeckKey(1, KeyKinds.Live, "Vor/Nach Spiel, Pause")), NoHalls).Validated().Buttons,
            b => b.Label == "Vor/Nach Spiel, Pause" && b.Action.Live is null);

    [Fact]
    public void A_broken_key_is_left_out_instead_of_stopping_the_start() =>
        Assert.DoesNotContain(TabBoardStart.Build(Setup(new DeckKey(1, KeyKinds.Music, "Ohne Musik")), NoHalls).Validated().Buttons,
            b => b.Label == "Ohne Musik");

    [Theory]
    [InlineData("Torbeschwörung", "torbeschwoerung")]
    [InlineData("Starting 6 Gast", "starting-6-gast")]
    [InlineData("LED Spiel L-UPL", "led-spiel-l-upl")]
    [InlineData("🚨", "button")]
    public void A_button_id_is_a_readable_slug(string label, string id) =>
        Assert.Equal(id, TabBoardStart.Slug(label));
}
