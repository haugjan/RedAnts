using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class DeckTests
{
    private static Deck Small(params DeckKey[] keys) =>
        new("test", "Test", 3, 2, [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)]);

    [Fact]
    public void Panic_sits_on_the_last_key_and_back_on_the_first_of_the_last_row()
    {
        var deck = Small();

        Assert.Equal(6, deck.Panic);
        Assert.Equal(4, deck.Back);
        Assert.True(deck.Reserved(6));
        Assert.True(deck.Reserved(4));
        Assert.False(deck.Reserved(1));
    }

    [Fact]
    public void A_deck_keeps_the_places_the_hall_chose()
    {
        var deck = Small() with { PanicKey = 25, BackKey = 17, Columns = 8, Rows = 4 };

        Assert.Equal(25, deck.Panic);
        Assert.Equal(17, deck.Back);
    }

    [Fact]
    public void A_key_outside_the_grid_is_refused()
    {
        var deck = Small(new DeckKey(7, KeyKinds.Steps, "zu weit"));

        var error = Assert.Throws<ValidationException>(() => deck.Validated());

        Assert.Contains("ausserhalb", error.Message);
    }

    [Fact]
    public void Panic_and_back_may_not_share_a_place()
    {
        var deck = Small() with { PanicKey = 3, BackKey = 3 };

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void A_deck_without_a_home_page_is_refused()
    {
        var deck = new Deck("test", "Test", 3, 2, [new DeckPage("anders", "Seite", null, [])]);

        var error = Assert.Throws<ValidationException>(() => deck.Validated());

        Assert.Contains("Startseite", error.Message);
    }

    [Fact]
    public void A_page_id_may_not_appear_twice()
    {
        var deck = new Deck("test", "Test", 3, 2,
        [
            new DeckPage(DeckPage.HomeId, "Startseite", null, []),
            new DeckPage("zwei", "Zwei", DeckPage.HomeId, []),
            new DeckPage("zwei", "Noch einmal", DeckPage.HomeId, []),
        ]);

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void A_special_key_needs_a_known_function()
    {
        var deck = Small(new DeckKey(1, KeyKinds.Special, "Tor", Special: "gibt-es-nicht"));

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void A_music_key_needs_a_command()
    {
        var deck = Small(new DeckKey(1, KeyKinds.Music, "Tor"));

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void A_page_key_needs_a_target()
    {
        var deck = Small(new DeckKey(1, KeyKinds.Page, "Meldung"));

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void An_unknown_command_in_a_sequence_is_refused()
    {
        var deck = Small(new DeckKey(1, KeyKinds.Steps, "Tor", Steps: [new KeyStep("tu-irgendwas")]));

        Assert.Throws<ValidationException>(() => deck.Validated());
    }

    [Fact]
    public void Keys_replace_each_other_by_number_and_stay_in_order()
    {
        var page = new DeckPage(DeckPage.HomeId, "Startseite", null,
            [new DeckKey(3, KeyKinds.Steps, "drei"), new DeckKey(1, KeyKinds.Steps, "eins")]);

        var changed = page.With(new DeckKey(3, KeyKinds.Steps, "neu"));

        Assert.Equal([1, 3], changed.Keys.Select(k => k.Number));
        Assert.Equal("neu", changed.KeyAt(3).Label);
        Assert.Equal(KeyKinds.Empty, changed.KeyAt(2).Kind);
    }

    [Fact]
    public void The_shipped_decks_are_valid_and_keep_the_places_of_the_console()
    {
        var field = DefaultDecks.Field().Validated();
        var music = DefaultDecks.Music().Validated();

        Assert.Equal(32, field.Count);
        Assert.Equal(25, field.Panic);
        Assert.Equal(17, field.Back);
        Assert.Equal(SpecialKey.GoalHome, field.Home.KeyAt(1).Special);
        Assert.Equal(SpecialKey.GoalAway, field.Home.KeyAt(8).Special);
        Assert.Equal(15, music.Count);
        Assert.Equal(11, music.Panic);
        Assert.Equal(1, music.Back);
        Assert.Equal(SpecialKey.AutoBreakMusic, music.Home.KeyAt(10).Special);
        Assert.Equal(SpecialKey.MusicStop, music.Home.KeyAt(12).Special);
        Assert.Equal(SpecialKey.MusicPause, music.Home.KeyAt(13).Special);
        Assert.Equal(SpecialKey.MusicNext, music.Home.KeyAt(14).Special);
        Assert.Equal(SpecialKey.MusicFade, music.Home.KeyAt(15).Special);
    }

    [Fact]
    public void Every_shipped_key_avoids_the_reserved_places()
    {
        foreach (var deck in DefaultDecks.All)
            foreach (var page in deck.Pages)
                foreach (var key in page.Keys)
                    Assert.False(deck.Reserved(key.Number, page.IsHome), $"{deck.Id}/{page.Id}: Taste {key.Number} liegt auf Zurück oder PANIC");
    }

    [Fact]
    public void A_sub_page_of_the_field_deck_returns_home_after_a_message()
    {
        var page = DefaultDecks.Field().Page(DefaultDecks.MessagePage);

        Assert.NotNull(page);
        Assert.Equal(DeckPage.HomeId, page!.ParentId);
        Assert.True(page.KeyAt(1).HomeAfter);
    }
}
