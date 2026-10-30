using RedAnts.Show.Agent.Cloud;

namespace RedAnts.Show.Agent.Tests;

public class DeckResolveTests
{
    static Hall With(params (string Id, string Name)[] decks) =>
        Hall.Offline with { Decks = decks.Select(d => new DeckLayout(d.Id, d.Name, 5, 3, [])).ToList() };

    [Fact]
    public void A_known_id_wins()
    {
        Assert.Equal("mk2", With(("xl", "Streamdeck XL (Full)"), ("mk2", "Streamdeck Mk2 (Full)")).ResolveDeck("MK2"));
    }

    [Fact]
    public void An_unknown_id_is_found_by_a_word_of_the_deck_name()
    {
        var hall = With(("deck-1", "Streamdeck XL (Full)"), ("deck-3", "Streamdeck Mk2 (Music Only)"));

        Assert.Equal("deck-3", hall.ResolveDeck("mk2"));
        Assert.Equal("deck-1", hall.ResolveDeck("xl"));
    }

    [Fact]
    public void A_hall_with_one_deck_answers_every_id()
    {
        Assert.Equal("deck-3", With(("deck-3", "Streamdeck Mk2 (Music Only)")).ResolveDeck("xl"));
    }

    [Fact]
    public void Without_a_match_among_several_decks_the_id_stays()
    {
        Assert.Equal("plus", With(("deck-1", "Streamdeck XL"), ("deck-3", "Streamdeck Mk2")).ResolveDeck("plus"));
    }

    [Fact]
    public void Without_an_id_the_first_deck_is_taken()
    {
        Assert.Equal("deck-1", With(("deck-1", "Streamdeck XL"), ("deck-3", "Streamdeck Mk2")).ResolveDeck(null));
    }
}
