using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class MusicTopUpTests
{
    private const string Folder = "Vor/Nach Spiel / Pause";

    private static MusicItem Piece(string id, string name) => new(id, name, "Bibliothek", 1, false);

    private static readonly IReadOnlyList<MusicItem> Library =
    [
        Piece("root-pause", "Pause"),
        Piece("timeout", "Time Out"),
        Piece("penalty", "Penalty"),
        Piece("break", "Spielunterbruch"),
        Piece("birthday", "Special Situations · Happy Birthday"),
        Piece("pause", $"{Folder} · Pause"),
        Piece("win", $"{Folder} · Nach Spiel (Sieg Red Ants)"),
        Piece("loss", $"{Folder} · Nach dem Spiel (Niederlage Red Ants)"),
        Piece("lupl1", $"{Folder} · Vor dem Spiel L-UPL · 1. Mobiliar Unihockey"),
        Piece("liga1", $"{Folder} · Vor dem Spiel 1. Liga · 1. Wartesong bis Teams bereit"),
        Piece("liga2", $"{Folder} · Vor dem Spiel 1. Liga · 2. Einlaufmusik (1 Liga)"),
        Piece("liga3", $"{Folder} · Vor dem Spiel 1. Liga · 3. Torbeschwörung"),
        Piece("liga4", $"{Folder} · Vor dem Spiel 1. Liga · 4. Starting Six Gegner"),
        Piece("liga5", $"{Folder} · Vor dem Spiel 1. Liga · 5. Starting Six Red Ants"),
        Piece("liga6", $"{Folder} · Vor dem Spiel 1. Liga · 6. Bully"),
    ];

    private static Deck Mk2() => new(DefaultDecks.MusicDeck, "Streamdeck Mk2 (DJ-Pult)", 5, 3,
    [
        new DeckPage(DeckPage.HomeId, "Startseite", null,
        [
            new DeckKey(1, KeyKinds.Page, "Vor/Nach Spiel / Pause", PageId: MusicTopUp.OverviewPage),
            new DeckKey(2, KeyKinds.Page, "Special Situations", PageId: MusicTopUp.SpecialPage),
            new DeckKey(5, KeyKinds.Music, "Spielunterbruch", MusicId: "break"),
            new DeckKey(10, KeyKinds.Special, "Automodus", Special: SpecialKey.AutoBreakMusic),
            .. DefaultDecks.Transport(),
        ]),
        new DeckPage(MusicTopUp.OverviewPage, "Vor/Nach Spiel / Pause", DeckPage.HomeId,
        [
            new DeckKey(2, KeyKinds.Page, "Vor Spiel L-UPL", PageId: "vor-lupl"),
            new DeckKey(3, KeyKinds.Page, "Pause L-UPL", PageId: "pause-lupl"),
            new DeckKey(4, KeyKinds.Page, "Nach Spiel L-UPL", PageId: "nach-lupl"),
            new DeckKey(5, KeyKinds.Live, "Vor/Nach Spiel, Pause", Live: ""),
            new DeckKey(7, KeyKinds.Page, "Vor Spiel U21", PageId: "vor-u21"),
            new DeckKey(8, KeyKinds.Page, "Pause U21", PageId: "pause-u21"),
            new DeckKey(9, KeyKinds.Page, "Nach Spiel U21", PageId: "nach-u21"),
            new DeckKey(10, KeyKinds.Music, "Aufwärmmusik", MusicId: "warm"),
            new DeckKey(12, KeyKinds.Page, "Vor Spiel U17", PageId: "vor-u17"),
            new DeckKey(13, KeyKinds.Page, "Pause U17", PageId: "pause-u17"),
            new DeckKey(14, KeyKinds.Page, "Nach Spiel U17", PageId: "nach-u17"),
        ]),
        new DeckPage("vor-lupl", "Vor Spiel L-UPL", MusicTopUp.OverviewPage,
        [
            new DeckKey(2, KeyKinds.Music, "1. Mobiliar Unihockey", MusicId: "lupl1"),
            new DeckKey(5, KeyKinds.Live, "Vor Spiel L-UPL", Live: ""),
            .. DefaultDecks.Transport(),
        ]),
        new DeckPage(MusicTopUp.SpecialPage, "Special Situations", DeckPage.HomeId,
        [
            new DeckKey(2, KeyKinds.Music, "Happy Birthday", MusicId: "birthday"),
            new DeckKey(5, KeyKinds.Live, "Special Situations", Live: ""),
            .. DefaultDecks.Transport(),
        ]),
    ], PanicKey: 11, BackKey: 1);

    private static Deck Filled() => MusicTopUp.Fill(Mk2(), Library)!;

    [Fact]
    public void Time_out_and_penalty_land_on_the_special_situations_page()
    {
        var special = Filled().Page(MusicTopUp.SpecialPage)!;

        Assert.Contains(special.Keys, key => key.MusicId == "timeout");
        Assert.Contains(special.Keys, key => key.MusicId == "penalty");
        Assert.Equal("birthday", special.KeyAt(2).MusicId);
    }

    [Fact]
    public void The_first_league_gets_the_same_three_pages_as_the_other_leagues()
    {
        var deck = Filled();

        var before = deck.Page(MusicTopUp.BeforePage)!;
        Assert.Equal(6, before.Keys.Count(key => key.Kind == KeyKinds.Music));
        Assert.Equal("liga1", before.KeyAt(2).MusicId);
        Assert.Equal("pause", deck.Page(MusicTopUp.BreakPage)!.KeyAt(2).MusicId);
        Assert.Equal(["win", "loss"], deck.Page(MusicTopUp.AfterPage)!.Keys
            .Where(key => key.Kind == KeyKinds.Music).Select(key => key.MusicId));
    }

    [Fact]
    public void The_first_league_hangs_under_the_page_that_holds_the_other_leagues()
    {
        var deck = Filled();

        Assert.Equal(MusicTopUp.OverviewPage, deck.Page(MusicTopUp.LeaguePage)!.ParentId);
        Assert.Contains(deck.Page(MusicTopUp.OverviewPage)!.Keys, key => key.PageId == MusicTopUp.LeaguePage);
        foreach (var id in new[] { MusicTopUp.BeforePage, MusicTopUp.BreakPage, MusicTopUp.AfterPage })
        {
            Assert.Equal(MusicTopUp.LeaguePage, deck.Page(id)!.ParentId);
            Assert.Contains(deck.Page(MusicTopUp.LeaguePage)!.Keys, key => key.PageId == id);
        }
    }

    [Fact]
    public void The_shared_pause_beats_a_nameless_leftover_of_the_same_name()
    {
        Assert.Equal("pause", Filled().Page(MusicTopUp.BreakPage)!.KeyAt(2).MusicId);
    }

    [Fact]
    public void Nothing_moves_and_the_deck_stays_valid()
    {
        var before = Mk2();
        var after = Filled();

        after.Validated();
        foreach (var page in before.Pages)
            foreach (var key in page.Keys)
                Assert.Equal(key, after.Page(page.Id)!.KeyAt(key.Number));
    }

    [Fact]
    public void A_second_run_adds_nothing()
    {
        Assert.Null(MusicTopUp.Fill(Filled(), Library));
    }
}
