using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class DeckFormatTests
{
    [Fact]
    public void Only_the_two_streamdeck_sizes_are_on_offer()
    {
        Assert.Equal([(5, 3), (8, 4)], DeckFormat.All.Select(format => (format.Columns, format.Rows)));
    }

    [Fact]
    public void A_new_deck_carries_the_size_and_a_start_page()
    {
        var format = DeckFormat.Of(DeckFormat.Mk2)!;

        var deck = format.Create("deck-4", "Neues Streamdeck Mk2").Validated();

        Assert.Equal(15, deck.Count);
        Assert.Equal(11, deck.Panic);
        Assert.Equal(1, deck.Back);
        Assert.True(Assert.Single(deck.Pages).IsHome);
    }

    [Fact]
    public void The_decks_in_use_match_a_size()
    {
        foreach (var deck in DefaultDecks.All)
            Assert.NotNull(DeckFormat.Of(deck));
    }

    [Fact]
    public void Decks_of_one_size_land_in_one_set()
    {
        var mk2 = DeckFormat.Of(DeckFormat.Mk2)!;
        var xl = DeckFormat.Of(DeckFormat.Xl)!;
        var page = new DeckPage("werbung", "Werbung", DeckPage.HomeId, []);
        var first = mk2.Create("a", "A") with { Pages = [new DeckPage(DeckPage.HomeId, "Startseite", null, []), page] };
        var second = mk2.Create("b", "B");
        var third = xl.Create("c", "C");

        var shared = DeckSharing.Share([first, second, third]);

        var set = Assert.Single(shared.Sets);
        Assert.True(set.Fits(second));
        Assert.False(set.Fits(third));
        Assert.Equal("werbung", Assert.Single(set.Pages).Id);
    }
}
