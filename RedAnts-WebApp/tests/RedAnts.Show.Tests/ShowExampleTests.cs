using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowExampleTests
{
    private static MusicNode Piece(string id, string name, string icon = "", string colour = "", string subtitle = "", int songs = 1, bool random = false) =>
        new(id, name, icon, colour, subtitle, songs, random, "[]", []);

    private static MusicNode Folder(string id, string name, params MusicNode[] children) =>
        new(id, name, "📁", "#1f242b", "", 0, false, "[]", children);

    private static readonly IReadOnlyList<MusicNode> Library =
    [
        Folder("f1", "Vor/Nach Spiel / Pause",
            Piece("m6", "Aufwärmmusik vor Spiel", "🔥", "#2f855a", "39 Songs", 39, true),
            Piece("m7", "2. Wartesong bis Teams bereit"),
            Piece("m8", "4. Einlaufmusik")),
        Folder("f2", "Special Situations", Piece("m14", "Happy Birthday")),
        Piece("m5", "Spielunterbruch", "🎵", "#f5b50a", "Queen", 91, true),
        Piece("m3", "Strafe Red Ants", "😬", "#d02d38", "Always Look"),
        Piece("m4", "Strafe Gegner", "😬", "#4a7fa5", "Sitzed Sie"),
        Piece("m13", "Penalty", "💥", "#f5b50a"),
        Piece("m1", "Tor Red Ants", "🥅", "#d02d38", "Samba"),
        Piece("m2", "Tor Gegner", "🥅", "#4a7fa5", "Fight Back"),
    ];

    [Fact]
    public void The_example_hall_offers_everything_and_carries_both_decks()
    {
        var hall = ShowExampleSeeder.Example(Library);

        Assert.Equal(ShowExampleSeeder.HallId, hall.Id);
        Assert.True(hall.Offers is { Sound: true, Clock: true, Vmix: true, Led: true });
        Assert.Equal(2, hall.DeckLayouts.Count);
        Assert.Equal("m5", hall.BreakMusicId);
        foreach (var deck in hall.DeckLayouts) deck.Validated();
    }

    [Fact]
    public void The_field_deck_plays_the_goal_and_penalty_music_of_the_club()
    {
        var hall = ShowExampleSeeder.Example(Library);
        var home = hall.DeckLayouts.First(d => d.Id == DefaultDecks.FieldDeck).Home;

        Assert.Equal("m1", home.KeyAt(1).Sequence[0].Target);
        Assert.Equal("m2", home.KeyAt(8).Sequence[0].Target);
        Assert.Equal("m3", home.KeyAt(9).Sequence[0].Target);
        Assert.Equal("m4", home.KeyAt(16).Sequence[0].Target);
        Assert.Equal(SpecialKey.GoalHome, home.KeyAt(1).Special);
    }

    [Fact]
    public void The_music_deck_mirrors_the_soundboard_with_its_names_colours_and_symbols()
    {
        var hall = ShowExampleSeeder.Example(Library);
        var deck = hall.DeckLayouts.First(d => d.Id == DefaultDecks.MusicDeck);
        var home = deck.Home;

        var folder = home.Keys.First(k => k.Kind == KeyKinds.Page);
        Assert.Equal("Vor/Nach Spiel / Pause", folder.Label);
        Assert.Equal("📁", folder.Icon);

        var piece = home.Keys.First(k => k.MusicId == "m5");
        Assert.Equal("Spielunterbruch", piece.Label);
        Assert.Equal("#f5b50a", piece.Color);
        Assert.Equal("🎵", piece.Icon);

        Assert.Contains(home.Keys, k => k.Special == SpecialKey.AutoBreakMusic);
        Assert.Contains(deck.Pages, p => p.Name == "Vor/Nach Spiel / Pause");
        Assert.Contains(deck.Pages.First(p => p.Name == "Vor/Nach Spiel / Pause").Keys, k => k.MusicId == "m6");
    }

    [Fact]
    public void A_sub_page_of_the_music_deck_leads_back_to_the_start()
    {
        var deck = ShowExampleSeeder.Example(Library).DeckLayouts.First(d => d.Id == DefaultDecks.MusicDeck);

        var folder = deck.Pages.First(p => p.Name == "Special Situations");

        Assert.Equal(DeckPage.HomeId, folder.ParentId);
        deck.Validated();
    }

    [Fact]
    public void Without_a_library_the_example_still_builds_a_valid_hall()
    {
        var hall = ShowExampleSeeder.Example([]);

        Assert.Equal("", hall.BreakMusicId);
        foreach (var deck in hall.DeckLayouts) deck.Validated();
    }
}
