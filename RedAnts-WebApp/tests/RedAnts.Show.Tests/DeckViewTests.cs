using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using RedAnts.Show.Features.Music;
using Xunit;

namespace RedAnts.Show.Tests;

public class DeckViewTests
{
    private static readonly MusicItem Goal = new("tor", "Tor Red Ants", "L-UPL", 1, false, "[{\"t\":\"local\"}]");

    private static Deck Deck(params DeckKey[] keys) =>
        new("mk2", "Mk2", 3, 2, [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)]);

    [Fact]
    public void Without_an_agent_the_board_shows_the_stored_page()
    {
        var deck = Deck(new DeckKey(1, KeyKinds.Music, "", MusicId: "tor"), new DeckKey(2, KeyKinds.Steps, "Opener"));

        var view = GetDeckView.Handler.Local(deck, [Goal]);

        Assert.Equal("mk2", view.DeckId);
        Assert.Equal(3, view.Columns);
        Assert.Equal(2, view.Rows);
        Assert.Equal(6, view.Keys.Count);
        Assert.False(view.FromAgent);
    }

    [Fact]
    public void A_music_key_without_a_label_shows_the_name_of_the_command()
    {
        var view = GetDeckView.Handler.Local(Deck(new DeckKey(1, KeyKinds.Music, "", MusicId: "tor")), [Goal]);

        var key = view.Keys[0];
        Assert.Equal("Tor Red Ants", key.Label);
        Assert.Equal("tor", key.MusicId);
        Assert.True(key.Pressable);
    }

    [Fact]
    public void A_music_key_whose_command_is_gone_cannot_be_pressed()
    {
        var view = GetDeckView.Handler.Local(Deck(new DeckKey(1, KeyKinds.Music, "Tor", MusicId: "weg")), [Goal]);

        Assert.False(view.Keys[0].Pressable);
        Assert.Null(view.Keys[0].MusicId);
    }

    [Fact]
    public void Panic_and_back_keep_their_places_even_without_an_agent()
    {
        var view = GetDeckView.Handler.Local(Deck(), []);

        Assert.Equal("PANIC", view.Keys[5].Label);
        Assert.True(view.Keys[5].Pressable);
        Assert.Equal("", view.Keys[3].Label);
        Assert.False(view.Keys[3].Pressable);
    }

    [Fact]
    public void A_key_that_needs_the_agent_stays_pressable_so_the_board_can_say_why()
    {
        var view = GetDeckView.Handler.Local(Deck(new DeckKey(1, KeyKinds.Special, "Tor", Special: SpecialKey.GoalHome)), []);

        Assert.True(view.Keys[0].Pressable);
    }

    [Fact]
    public void An_empty_place_stays_black_and_dead()
    {
        var view = GetDeckView.Handler.Local(Deck(), []);

        Assert.Equal("#000000", view.Keys[0].Color);
        Assert.False(view.Keys[0].Pressable);
    }
}
