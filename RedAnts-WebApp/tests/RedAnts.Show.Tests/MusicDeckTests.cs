using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class MusicDeckTests
{
    private static MusicNode Piece(string id, string name) =>
        new(id, name, "🎵", "#f5b50a", "", 1, false, "[]", []);

    private static MusicNode Folder(string id, string name, params MusicNode[] children) =>
        new(id, name, "📁", "#1f242b", "", 0, false, "[]", children);

    private static MusicNode Control(string id, string name, string control) =>
        new(id, name, "", "", "", 0, false, "[]", [], Control: control);

    private static readonly IReadOnlyList<MusicNode> Library =
    [
        Piece("unterbruch", "Spielunterbruch"),
        Folder("situations", "Special Situations",
            Piece("birthday", "Happy Birthday"),
            Piece("hei", "Bring En Hei (Verlängerung)"),
            Control("c1", "Pause", "pause")),
        Folder("vornach", "Vor/Nach Spiel / Pause",
            Folder("lupl", "Vor dem Spiel L-UPL",
                Piece("l1", "1. Mobiliar Unihockey"),
                Piece("l2", "2. Wartesong bis Teams bereit"),
                Piece("l3", "3. LIDL L-UPL"),
                Piece("l4", "4. Einlaufmusik (L-UPL, andere siehe unten)"),
                Piece("l5", "5. Torbeschwörung"),
                Piece("l6", "6. Starting Six Gegner"),
                Piece("l7", "7. Starting Six Red Ants"),
                Piece("l8", "8. Bully"),
                Control("c2", "Fade-out", "fade")),
            Folder("u21", "Vor dem Spiel U21",
                Piece("a1", "1. Wartesong bis Teams bereit"),
                Piece("a2", "2. Einlaufmusik (U21)"),
                Piece("a3", "3. Torbeschwörung"),
                Piece("a4", "4. Starting Six Gegner"),
                Piece("a5", "5. Starting Six Red Ants"),
                Piece("a6", "6. Bully")),
            Folder("u17", "Vor dem Spiel U17",
                Piece("b1", "1. Wartesong bis Teams bereit"),
                Piece("b2", "2. Einlaufmusik (U17)"),
                Piece("b3", "3. Torbeschwörung"),
                Piece("b4", "4. Starting Six Gegner"),
                Piece("b5", "5. Starting Six Red Ants"),
                Piece("b6", "6. Bully")),
            Piece("warm", "Aufwärmmusik vor Spiel"),
            Piece("pause", "Pause"),
            Piece("sieg", "Nach Spiel (Sieg Red Ants)"),
            Piece("niederlage", "Nach dem Spiel (Niederlage Red Ants)"),
            Piece("lidlpause", "Lidl Pausendurchsage (nur L-UPL)"),
            Piece("lidlnach", "Lidl nach Spiel (nur L-UPL)")),
    ];

    private static Deck Built() => MusicDeck.Build(DefaultDecks.Music(), Library).Validated();

    [Fact]
    public void The_start_page_carries_the_two_folders_the_break_music_and_the_transport()
    {
        var home = Built().Home;

        Assert.Equal(MusicDeck.BeforeAndAfter, home.KeyAt(1).PageId);
        Assert.Equal(MusicDeck.Special, home.KeyAt(2).PageId);
        Assert.Equal("unterbruch", home.KeyAt(5).MusicId);
        Assert.Equal(SpecialKey.AutoBreakMusic, home.KeyAt(10).Special);
        Assert.Equal(SpecialKey.MusicStop, home.KeyAt(12).Special);
        Assert.Equal(SpecialKey.MusicPause, home.KeyAt(13).Special);
        Assert.Equal(SpecialKey.MusicNext, home.KeyAt(14).Special);
        Assert.Equal(SpecialKey.MusicFade, home.KeyAt(15).Special);
    }

    [Fact]
    public void Panic_sits_on_eleven_and_back_on_one()
    {
        var deck = Built();

        Assert.Equal(11, deck.Panic);
        Assert.Equal(1, deck.Back);
        Assert.False(deck.Reserved(1, home: true));
        Assert.True(deck.Reserved(1));
    }

    [Fact]
    public void The_second_page_opens_one_page_per_league_and_moment()
    {
        var page = Built().Page(MusicDeck.BeforeAndAfter);

        Assert.NotNull(page);
        Assert.Equal(DeckPage.HomeId, page!.ParentId);
        Assert.Equal(["vor-lupl", "pause-lupl", "nach-lupl"], new[] { 2, 3, 4 }.Select(n => page.KeyAt(n).PageId));
        Assert.Equal(["vor-u21", "pause-u21", "nach-u21"], new[] { 7, 8, 9 }.Select(n => page.KeyAt(n).PageId));
        Assert.Equal(["vor-u17", "pause-u17", "nach-u17"], new[] { 12, 13, 14 }.Select(n => page.KeyAt(n).PageId));
        Assert.Equal("warm", page.KeyAt(10).MusicId);
        Assert.Equal(KeyKinds.Live, page.KeyAt(5).Kind);
        Assert.Equal("Vor/Nach Spiel, Pause", page.KeyAt(5).Label);
    }

    [Fact]
    public void Before_the_game_the_league_keeps_its_own_order_with_sponsors_only_in_the_top_league()
    {
        var deck = Built();

        var lupl = deck.Page("vor-lupl")!;
        Assert.Equal(["l1", "l2", "l3", "l4", "l5", "l6", "l7", "l8"],
            new[] { 2, 3, 4, 6, 7, 8, 9, 10 }.Select(n => lupl.KeyAt(n).MusicId));

        var u21 = deck.Page("vor-u21")!;
        Assert.Equal(["a1", "a2", "a3", "a4", "a5", "a6"],
            new[] { 2, 3, 4, 6, 7, 8 }.Select(n => u21.KeyAt(n).MusicId));
        Assert.True(u21.KeyAt(9).IsEmpty);
        Assert.True(u21.KeyAt(10).IsEmpty);
    }

    [Fact]
    public void The_break_and_the_end_carry_the_sponsor_spot_only_for_the_top_league()
    {
        var deck = Built();

        Assert.Equal("lidlpause", deck.Page("pause-lupl")!.KeyAt(2).MusicId);
        Assert.Equal("pause", deck.Page("pause-lupl")!.KeyAt(3).MusicId);
        Assert.Equal("pause", deck.Page("pause-u17")!.KeyAt(2).MusicId);

        Assert.Equal("lidlnach", deck.Page("nach-lupl")!.KeyAt(2).MusicId);
        Assert.Equal(["sieg", "niederlage"], new[] { 3, 4 }.Select(n => deck.Page("nach-lupl")!.KeyAt(n).MusicId));
        Assert.Equal(["sieg", "niederlage"], new[] { 2, 3 }.Select(n => deck.Page("nach-u21")!.KeyAt(n).MusicId));
    }

    [Fact]
    public void Every_sub_page_shows_its_name_on_a_dead_key_and_the_same_bottom_row()
    {
        var deck = Built();

        foreach (var page in deck.Pages.Where(p => !p.IsHome && p.Id != MusicDeck.BeforeAndAfter))
        {
            Assert.Equal(KeyKinds.Live, page.KeyAt(5).Kind);
            Assert.Equal(page.Name, page.KeyAt(5).Label);
            Assert.Equal(SpecialKey.MusicStop, page.KeyAt(12).Special);
            Assert.Equal(SpecialKey.MusicPause, page.KeyAt(13).Special);
            Assert.Equal(SpecialKey.MusicNext, page.KeyAt(14).Special);
            Assert.Equal(SpecialKey.MusicFade, page.KeyAt(15).Special);
        }
    }

    [Fact]
    public void Special_situations_hang_under_the_start_page_and_leave_the_controls_out()
    {
        var page = Built().Page(MusicDeck.Special);

        Assert.NotNull(page);
        Assert.Equal(DeckPage.HomeId, page!.ParentId);
        Assert.Equal(["birthday", "hei"], new[] { 2, 3 }.Select(n => page.KeyAt(n).MusicId));
        Assert.True(page.KeyAt(4).IsEmpty);
    }

    [Fact]
    public void An_existing_hall_gets_the_new_pages_once_and_then_stays_untouched()
    {
        var hall = Hall.Create("win4", "WIN4 Stratos") with { Decks = [DefaultDecks.Field(), DefaultDecks.Music()] };

        var updated = ShowExampleSeeder.WithNewMusicDeck(hall, Library);

        Assert.NotNull(updated);
        Assert.Equal(2, updated!.DeckLayouts.Count);
        var music = updated.DeckLayouts.First(d => d.Id == DefaultDecks.MusicDeck);
        Assert.NotNull(music.Page(MusicDeck.BeforeAndAfter));
        Assert.Equal(11, music.Panic);
        Assert.Null(ShowExampleSeeder.WithNewMusicDeck(updated, Library));
    }

    [Fact]
    public void A_hall_without_a_music_deck_is_left_alone()
    {
        var hall = Hall.Create("oberseen", "Oberseen") with { Decks = [DefaultDecks.Field()] };

        Assert.Null(ShowExampleSeeder.WithNewMusicDeck(hall, Library));
    }

    [Fact]
    public void Without_a_library_the_deck_still_validates()
    {
        var deck = MusicDeck.Build(DefaultDecks.Music(), []).Validated();

        Assert.Equal(SpecialKey.MusicStop, deck.Home.KeyAt(12).Special);
        Assert.True(deck.Home.KeyAt(5).IsEmpty);
    }
}
