using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure.Content;
using Xunit;

namespace RedAnts.Show.Tests;

public class TeamLogoTopUpTests
{
    private static Deck Field(params DeckKey[] keys) =>
        new(DefaultDecks.FieldDeck, "Streamdeck XL (Full)", 8, 4,
            [new DeckPage(DeckPage.HomeId, "Startseite", null, keys)], PanicKey: 25, BackKey: 17);

    private static ShowSetup Setup(Deck deck, params DeckPage[] shared) =>
        new([], Hall.DefaultLedTemplates, [deck], [new PageSet(8, 4, shared)]);

    private static DeckKey[] HallDeck() =>
    [
        .. Enumerable.Range(1, 18).Select(number => new DeckKey(number, KeyKinds.Steps, $"Taste {number}")),
        new DeckKey(19, KeyKinds.Live, "Drittel", Live: LiveValues.Period),
        new DeckKey(20, KeyKinds.Live, "Stand Heim", Live: LiveValues.HomeScore),
        new DeckKey(21, KeyKinds.Live, "Stand Gast", Live: LiveValues.AwayScore),
        new DeckKey(22, KeyKinds.Empty),
        new DeckKey(23, KeyKinds.Empty),
        .. Enumerable.Range(24, 9).Where(number => number != 25).Select(number => new DeckKey(number, KeyKinds.Steps, $"Taste {number}")),
    ];

    [Fact]
    public void The_hall_deck_loses_the_period_and_gains_the_two_logo_keys()
    {
        var filled = TeamLogoTopUp.Fill(Setup(Field(HallDeck())));

        Assert.NotNull(filled);
        var home = filled!.DeckLibrary[0].Home;
        Assert.DoesNotContain(home.Keys, key => key.Live == LiveValues.Period);
        Assert.Equal("team-redants", home.KeyAt(19).Icon);
        Assert.Equal(SpecialKey.GuestLogo, home.KeyAt(TeamLogoTopUp.GuestPlace).Special);
        Assert.Equal(DefaultDecks.TeamLogoPage, home.KeyAt(TeamLogoTopUp.PagePlace).PageId);
        Assert.Equal("Taste 17", home.KeyAt(17).Label);
    }

    [Fact]
    public void The_two_score_keys_become_one_and_free_a_place()
    {
        var home = TeamLogoTopUp.Fill(Setup(Field(HallDeck())))!.DeckLibrary[0].Home;

        Assert.Equal(LiveValues.Score, home.KeyAt(20).Live);
        Assert.DoesNotContain(home.Keys, key => key.Live is LiveValues.HomeScore or LiveValues.AwayScore);
        Assert.True(home.KeyAt(21).IsEmpty);
    }

    [Fact]
    public void A_deck_with_only_one_score_key_keeps_it()
    {
        var keys = HallDeck().Where(key => key.Live != LiveValues.AwayScore).ToArray();

        var home = TeamLogoTopUp.Fill(Setup(Field(keys)))!.DeckLibrary[0].Home;

        Assert.Equal(LiveValues.HomeScore, home.KeyAt(20).Live);
    }

    [Fact]
    public void On_a_deck_whose_last_free_place_is_taken_the_entry_uses_the_one_the_score_frees()
    {
        var keys = HallDeck().Select(key => key.Number == TeamLogoTopUp.PagePlace
            ? new DeckKey(TeamLogoTopUp.PagePlace, KeyKinds.Steps, "Replay")
            : key).ToArray();

        var home = TeamLogoTopUp.Fill(Setup(Field(keys)))!.DeckLibrary[0].Home;

        Assert.Equal("Replay", home.KeyAt(TeamLogoTopUp.PagePlace).Label);
        Assert.Equal(SpecialKey.GuestLogo, home.KeyAt(TeamLogoTopUp.GuestPlace).Special);
        Assert.Equal(DefaultDecks.TeamLogoPage, home.KeyAt(21).PageId);
        Assert.Equal(LiveValues.Score, home.KeyAt(20).Live);
    }

    [Fact]
    public void The_page_lands_in_the_shared_pages_and_carries_every_team()
    {
        var filled = TeamLogoTopUp.Fill(Setup(Field(HallDeck())));

        var page = Assert.Single(filled!.PageLibrary[0].Pages);
        Assert.Equal(DefaultDecks.TeamLogoPage, page.Id);
        Assert.Equal(11, page.Keys.Count);
    }

    [Fact]
    public void A_deck_without_the_period_keeps_its_keys_and_only_gains_the_logos()
    {
        var keys = HallDeck().Where(key => key.Live != LiveValues.Period).ToArray();

        var home = TeamLogoTopUp.Fill(Setup(Field(keys)))!.DeckLibrary[0].Home;

        Assert.True(home.KeyAt(19).IsEmpty);
        Assert.Equal(SpecialKey.GuestLogo, home.KeyAt(TeamLogoTopUp.GuestPlace).Special);
    }

    private static DeckPage LedPage() =>
        new("led", "Vor Spiel/Pause", DeckPage.HomeId,
            [new DeckKey(1, KeyKinds.Steps, "LED Pause", Steps: [new KeyStep(KeyActions.LedStart, "led:pause")])]);

    [Fact]
    public void With_one_place_left_the_guest_key_gets_it_and_the_entry_joins_the_led_keys()
    {
        var keys = Enumerable.Range(1, 32).Where(number => number is not (22 or 25))
            .Select(number => number == 24
                ? new DeckKey(24, KeyKinds.Page, "Vor Spiel/Pause", PageId: "led")
                : new DeckKey(number, KeyKinds.Steps, $"Taste {number}"))
            .ToArray();

        var filled = TeamLogoTopUp.Fill(Setup(Field(keys), LedPage()))!;

        var home = filled.DeckLibrary[0].Home;
        Assert.Equal(SpecialKey.GuestLogo, home.KeyAt(TeamLogoTopUp.GuestPlace).Special);
        Assert.DoesNotContain(home.Keys, key => key.PageId == DefaultDecks.TeamLogoPage);

        var led = filled.PageLibrary[0].Pages.Single(page => page.Id == "led");
        Assert.Equal(DefaultDecks.TeamLogoPage, led.KeyAt(TeamLogoTopUp.PagePlace).PageId);
        Assert.Single(filled.PageLibrary[0].Pages, page => page.Id == "led");
    }

    [Fact]
    public void A_start_page_with_room_keeps_the_entry_at_hand()
    {
        var filled = TeamLogoTopUp.Fill(Setup(Field(HallDeck()), LedPage()))!;

        Assert.Equal(DefaultDecks.TeamLogoPage, filled.DeckLibrary[0].Home.KeyAt(TeamLogoTopUp.PagePlace).PageId);
        Assert.DoesNotContain(filled.PageLibrary[0].Pages.Single(page => page.Id == "led").Keys,
            key => key.PageId == DefaultDecks.TeamLogoPage);
    }

    [Fact]
    public void Running_it_a_second_time_changes_nothing()
    {
        var once = TeamLogoTopUp.Fill(Setup(Field(HallDeck())));

        Assert.Null(TeamLogoTopUp.Fill(once!));
    }

    [Fact]
    public void Decks_of_another_size_stay_untouched()
    {
        var show = new ShowSetup([], Hall.DefaultLedTemplates, [DefaultDecks.Music()], [new PageSet(5, 3, [])]);

        Assert.Null(TeamLogoTopUp.Fill(show));
    }

    [Fact]
    public void Without_shared_pages_the_page_lands_on_the_deck_itself()
    {
        var show = new ShowSetup([], Hall.DefaultLedTemplates, [Field(HallDeck())], []);

        var deck = TeamLogoTopUp.Fill(show)!.DeckLibrary[0].Validated();

        Assert.NotNull(deck.Page(DefaultDecks.TeamLogoPage));
    }

    [Fact]
    public void The_shipped_deck_needs_no_change()
    {
        var show = new ShowSetup([], Hall.DefaultLedTemplates, [DefaultDecks.Field()], []);

        Assert.Null(TeamLogoTopUp.Fill(show));
    }
}
