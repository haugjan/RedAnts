using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowExampleTests
{
    private static readonly IReadOnlyList<MusicItem> Library =
    [
        new("m1", "Tor Red Ants", "L-UPL", 1, false),
        new("m2", "Tor Gegner", "L-UPL", 1, false),
        new("m3", "Strafe Red Ants", "L-UPL", 1, false),
        new("m4", "Strafe Gegner", "L-UPL", 1, false),
        new("m5", "Spielunterbruch", "L-UPL", 91, true),
        new("m6", "Aufwärmmusik vor Spiel", "L-UPL · Pause", 39, true),
        new("m7", "2. Wartesong bis Teams bereit", "L-UPL · Vor dem Spiel", 1, false),
        new("m8", "4. Einlaufmusik", "L-UPL · Vor dem Spiel", 1, false),
        new("m9", "5. Torbeschwörung", "L-UPL · Vor dem Spiel", 1, false),
        new("m10", "8. Bully", "L-UPL · Vor dem Spiel", 1, false),
        new("m11", "6. Starting Six Gegner", "L-UPL · Vor dem Spiel", 1, false),
        new("m12", "7. Starting Six Red Ants", "L-UPL · Vor dem Spiel", 1, false),
        new("m13", "Penalty", "L-UPL", 1, false),
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
    public void The_music_deck_carries_the_pieces_the_dj_reaches_for()
    {
        var hall = ShowExampleSeeder.Example(Library);
        var home = hall.DeckLayouts.First(d => d.Id == DefaultDecks.MusicDeck).Home;

        Assert.Equal("m6", home.KeyAt(1).MusicId);
        Assert.Equal("m5", home.KeyAt(8).MusicId);
        Assert.Equal("m13", home.KeyAt(9).MusicId);
        Assert.Equal(KeyKinds.Music, home.KeyAt(1).Kind);
        Assert.Equal(SpecialKey.AutoBreakMusic, home.KeyAt(14).Special);
    }

    [Fact]
    public void Without_a_library_the_example_still_builds_a_valid_hall()
    {
        var hall = ShowExampleSeeder.Example([]);

        Assert.Equal("", hall.BreakMusicId);
        foreach (var deck in hall.DeckLayouts) deck.Validated();
    }
}
