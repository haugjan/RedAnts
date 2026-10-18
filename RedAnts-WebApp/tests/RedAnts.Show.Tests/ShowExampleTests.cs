using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowExampleTests
{
    private static MusicItem Piece(string id, string name, int songs = 1, bool random = false) =>
        new(id, name, "", songs, random);

    private static readonly IReadOnlyList<MusicItem> Library =
    [
        Piece("m6", "Vor/Nach Spiel / Pause · Aufwärmmusik vor Spiel", 39, true),
        Piece("m7", "Vor/Nach Spiel / Pause · 2. Wartesong bis Teams bereit"),
        Piece("m8", "Vor/Nach Spiel / Pause · 4. Einlaufmusik"),
        Piece("m14", "Special Situations · Happy Birthday"),
        Piece("m5", "Spielunterbruch", 91, true),
        Piece("m3", "Strafe Red Ants"),
        Piece("m4", "Strafe Gegner"),
        Piece("m13", "Penalty"),
        Piece("m1", "Tor Red Ants"),
        Piece("m2", "Tor Gegner"),
        Piece("m9", "Time Out"),
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
    public void A_name_from_a_former_folder_is_found_by_its_last_part()
    {
        var hall = ShowExampleSeeder.Example(Library);
        var home = hall.DeckLayouts.First(d => d.Id == DefaultDecks.FieldDeck).Home;

        Assert.Equal("m9", home.KeyAt(3).Sequence[0].Target);
    }

    [Fact]
    public void The_music_deck_starts_empty_and_is_filled_under_decks()
    {
        var hall = ShowExampleSeeder.Example(Library);
        var deck = hall.DeckLayouts.First(d => d.Id == DefaultDecks.MusicDeck);

        Assert.All(deck.Home.Keys.Where(k => !deck.Reserved(k.Number)), key => Assert.NotEqual(KeyKinds.Music, key.Kind));
        deck.Validated();
    }

    [Fact]
    public void Every_shipped_hall_offers_what_its_name_promises()
    {
        var sound = ShowExampleSeeder.SoundOnly("win4-sound", "WIN4 nur Sound", Library).Validated();
        Assert.True(sound.Offers is { Sound: true, Clock: false, Vmix: false, Led: false });
        Assert.Single(sound.DeckLayouts);

        var notebook = ShowExampleSeeder.Notebook(Library).Validated();
        Assert.True(notebook.Offers is { Sound: true, Clock: true, Vmix: false, Led: false });
        Assert.Equal("Bodet", notebook.Devices.ClockType);
        Assert.Equal(4001, notebook.Devices.ClockPort);

        var stream = ShowExampleSeeder.StreamOnly(Library).Validated();
        Assert.True(stream.Offers is { Sound: false, Clock: true, Vmix: true, Led: false });
        Assert.Equal("", stream.BreakMusicId);
        Assert.Equal(DefaultDecks.FieldDeck, Assert.Single(stream.DeckLayouts).Id);
    }

    [Fact]
    public void Each_hall_carries_its_own_agent_key()
    {
        var one = ShowExampleSeeder.SoundOnly("oberseen", "Oberseen iPad", Library);
        var other = ShowExampleSeeder.SoundOnly("rennweg-ipad", "Rennweg iPad", Library);

        Assert.NotEqual(one.AgentKey, other.AgentKey);
        Assert.NotEqual(one.Id, other.Id);
    }

    [Fact]
    public void Without_a_library_the_example_still_builds_a_valid_hall()
    {
        var hall = ShowExampleSeeder.Example([]);

        Assert.Equal("", hall.BreakMusicId);
        foreach (var deck in hall.DeckLayouts) deck.Validated();
    }
}
