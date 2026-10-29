using RedAnts.Domain;
using RedAnts.Show.Domain;
using Xunit;

namespace RedAnts.Show.Tests;

public class DeckSharingTests
{
    private static Deck Small(string id, string name, params DeckPage[] pages) =>
        new(id, name, 5, 3, pages, PanicKey: 11, BackKey: 1);

    private static DeckPage Home(params DeckKey[] keys) => new(DeckPage.HomeId, "Startseite", null, keys);

    private static DeckKey Opens(int number, string pageId) => new(number, KeyKinds.Page, "Seite", PageId: pageId);

    private static DeckKey Song(int number, string musicId) => new(number, KeyKinds.Music, "Stück", MusicId: musicId);

    private static PageSet SetOf(DeckSharing.Library shared, int columns, int rows) =>
        Assert.Single(shared.Sets, set => set.Columns == columns && set.Rows == rows);

    [Fact]
    public void A_sub_page_moves_into_the_set_of_its_raster()
    {
        var deck = Small("mk2", "Mk2", Home(Opens(2, "werbung")), new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")]));

        var shared = DeckSharing.Share([deck]);

        var kept = Assert.Single(shared.Decks);
        Assert.Equal([DeckPage.HomeId], kept.Pages.Select(page => page.Id));
        var set = SetOf(shared, 5, 3);
        Assert.Equal("Werbung", Assert.Single(set.Pages).Name);
        Assert.Equal("werbung", kept.Home.KeyAt(2).PageId);
    }

    [Fact]
    public void Two_decks_of_the_same_raster_keep_one_copy_of_an_identical_page()
    {
        var page = new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")]);
        var full = Small("mk2", "Mk2 Full", Home(Opens(2, "werbung")), page);
        var music = Small("deck-3", "Mk2 Music Only", Home(Opens(4, "werbung")), page with { Name = "Werbung kurz" });

        var shared = DeckSharing.Share([full, music]);

        var set = SetOf(shared, 5, 3);
        Assert.Equal("Werbung", Assert.Single(set.Pages).Name);
        Assert.Equal("werbung", shared.Decks[0].Home.KeyAt(2).PageId);
        Assert.Equal("werbung", shared.Decks[1].Home.KeyAt(4).PageId);
    }

    [Fact]
    public void A_page_that_differs_stays_next_to_the_other_and_carries_the_deck_name()
    {
        var full = Small("mk2", "Mk2 Full",
            Home(Opens(2, "werbung")),
            new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")]));
        var music = Small("deck-3", "Mk2 Music Only",
            Home(Opens(2, "werbung")),
            new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "gastro")]));

        var shared = DeckSharing.Share([full, music], null, tagNames: true);

        var set = SetOf(shared, 5, 3);
        Assert.Equal(["werbung", "werbung-deck-3"], set.Pages.Select(page => page.Id));
        Assert.Equal(["Werbung · Mk2 Full", "Werbung · Mk2 Music Only"], set.Pages.Select(page => page.Name));
        Assert.Equal("werbung", shared.Decks[0].Home.KeyAt(2).PageId);
        Assert.Equal("werbung-deck-3", shared.Decks[1].Home.KeyAt(2).PageId);
        Assert.Equal("gastro", set.Page("werbung-deck-3")!.KeyAt(3).MusicId);
    }

    [Fact]
    public void A_page_that_opens_a_page_of_its_own_deck_follows_it()
    {
        var full = Small("mk2", "Mk2 Full",
            Home(Opens(2, "liga")),
            new DeckPage("liga", "L-UPL", DeckPage.HomeId, [Opens(3, "pause")]),
            new DeckPage("pause", "Pause", "liga", [Song(4, "jingle")]));
        var music = Small("deck-3", "Mk2 Music Only",
            Home(Opens(2, "liga")),
            new DeckPage("liga", "L-UPL", DeckPage.HomeId, [Opens(3, "pause")]),
            new DeckPage("pause", "Pause", "liga", [Song(4, "gastro")]));

        var shared = DeckSharing.Share([full, music]);

        var set = SetOf(shared, 5, 3);
        Assert.Equal(["liga", "pause", "liga-deck-3", "pause-deck-3"], set.Pages.Select(page => page.Id));
        Assert.Equal("liga-deck-3", shared.Decks[1].Home.KeyAt(2).PageId);
        Assert.Equal("pause-deck-3", set.Page("liga-deck-3")!.KeyAt(3).PageId);
        Assert.Equal("pause", set.Page("liga")!.KeyAt(3).PageId);
    }

    [Fact]
    public void Decks_of_another_raster_keep_their_pages_apart()
    {
        var small = Small("mk2", "Mk2", Home(), new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")]));
        var big = new Deck("xl", "XL", 8, 4,
            [Home(), new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(20, "gastro")])], PanicKey: 25, BackKey: 17);

        var shared = DeckSharing.Share([small, big]);

        Assert.Equal(2, shared.Sets.Count);
        Assert.Equal("jingle", SetOf(shared, 5, 3).Page("werbung")!.KeyAt(3).MusicId);
        Assert.Equal("gastro", SetOf(shared, 8, 4).Page("werbung")!.KeyAt(20).MusicId);
    }

    [Fact]
    public void Sharing_an_already_shared_library_changes_nothing()
    {
        var full = Small("mk2", "Mk2 Full", Home(Opens(2, "werbung")), new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")]));
        var music = Small("deck-3", "Mk2 Music Only", Home(Opens(2, "werbung")), new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "gastro")]));
        var once = DeckSharing.Share([full, music], null, tagNames: true);

        var twice = DeckSharing.Share(once.Decks, once.Sets, tagNames: true);

        Assert.Equal(once.Sets[0].Pages.Select(page => page.Id), twice.Sets[0].Pages.Select(page => page.Id));
        Assert.Equal(once.Sets[0].Pages.Select(page => page.Name), twice.Sets[0].Pages.Select(page => page.Name));
        Assert.Equal(once.Decks.Select(deck => deck.Home.KeyAt(2).PageId), twice.Decks.Select(deck => deck.Home.KeyAt(2).PageId));
    }

    [Fact]
    public void A_deck_reads_the_pages_of_its_raster()
    {
        var deck = Small("deck-3", "Mk2 Music Only", Home(Opens(2, "werbung")));
        var set = new PageSet(5, 3, [new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")])]);
        var setup = new ShowSetup([], [], [deck], [set]);

        var whole = setup.Complete(deck);

        Assert.Equal([DeckPage.HomeId, "werbung"], whole.Pages.Select(page => page.Id));
        Assert.Equal("jingle", whole.Page("werbung")!.KeyAt(3).MusicId);
    }

    [Fact]
    public void A_deck_without_a_set_keeps_the_pages_it_carries()
    {
        var deck = Small("mk2", "Mk2", Home(Opens(2, "werbung")), new DeckPage("werbung", "Werbung", DeckPage.HomeId, [Song(3, "jingle")]));
        var setup = new ShowSetup([], [], [deck]);

        Assert.Equal([DeckPage.HomeId, "werbung"], setup.Complete(deck).Pages.Select(page => page.Id));
    }

    [Fact]
    public void A_set_takes_no_start_page()
    {
        var set = new PageSet(5, 3, [Home()]);

        Assert.Throws<ValidationException>(() => set.Validated());
    }
}
