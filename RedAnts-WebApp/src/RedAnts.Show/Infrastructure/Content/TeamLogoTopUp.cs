using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TeamLogoTopUp
{
    public const string SettingKey = "Show:DeckTeamLogos";
    public const string Version = "1-lupl-damen";

    public const int GuestPlace = 22;
    public const int PagePlace = 23;

    private const int Columns = 8;
    private const int Rows = 4;

    public static ShowSetup? Fill(ShowSetup show)
    {
        var decks = show.DeckLibrary.ToList();
        var sets = show.PageLibrary.ToList();
        var changed = false;

        for (var index = 0; index < decks.Count; index++)
        {
            var deck = decks[index];
            if (deck.Columns != Columns || deck.Rows != Rows) continue;

            var set = sets.FindIndex(known => known.Fits(deck));
            if (!Carries(show, deck))
            {
                var page = DefaultDecks.TeamLogos();
                if (set >= 0) sets[set] = sets[set].With(page);
                else deck = deck with { Pages = [.. deck.Pages, page] };
                changed = true;
            }

            if (Arranged(deck) is { } home)
            {
                deck = deck with { Pages = deck.Pages.Select(page => page.IsHome ? home : page).ToList() };
                changed = true;
            }

            if (!Opens(show.PagesOf(deck)) && Beside(show, deck) is { } led)
            {
                if (Place(deck, led, PagePlace) is not { } spot) { decks[index] = deck; continue; }
                var moved = led.With(DefaultDecks.TeamLogoKey(spot));
                if (set >= 0 && sets[set].Page(moved.Id) is not null) sets[set] = sets[set].With(moved);
                else deck = deck with { Pages = deck.Pages.Select(page => page.Id == moved.Id ? moved : page).ToList() };
                changed = true;
            }
            decks[index] = deck;
        }

        return changed ? show with { Decks = decks, Sheets = sets } : null;
    }

    private static bool Opens(IReadOnlyList<DeckPage> pages) =>
        pages.SelectMany(page => page.Keys)
            .Any(key => DefaultDecks.TeamLogoPage.Equals(key.PageId, StringComparison.OrdinalIgnoreCase));

    private static DeckPage? Beside(ShowSetup show, Deck deck)
    {
        var pages = show.PagesOf(deck);
        var reachable = deck.Home.Keys.Select(key => key.PageId).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return pages
            .Where(page => reachable.Contains(page.Id) && !page.Id.Equals(DefaultDecks.TeamLogoPage, StringComparison.OrdinalIgnoreCase))
            .Select(page => (Page: page, Leds: page.Keys.Count(Led)))
            .Where(found => found.Leds > 0)
            .OrderByDescending(found => found.Leds)
            .ThenBy(found => found.Page.Id, StringComparer.OrdinalIgnoreCase)
            .Select(found => found.Page)
            .FirstOrDefault();
    }

    private static bool Led(DeckKey key) =>
        key.Sequence.Any(step => step.Action == KeyActions.LedStart);

    private static bool Carries(ShowSetup show, Deck deck) =>
        show.PagesOf(deck).Any(page => page.Id.Equals(DefaultDecks.TeamLogoPage, StringComparison.OrdinalIgnoreCase));

    private static DeckPage? Arranged(Deck deck)
    {
        var home = deck.Home;
        var before = home;

        if (home.Keys.FirstOrDefault(Shows(LiveValues.Period)) is { } period)
            home = home.With(DefaultDecks.RedAntsLogoKey(period.Number));

        if (home.Keys.FirstOrDefault(Shows(LiveValues.HomeScore)) is { } scored
            && home.Keys.FirstOrDefault(Shows(LiveValues.AwayScore)) is { } conceded)
        {
            home = home.With(DefaultDecks.ScoreKey(scored.Number));
            home = home.With(new DeckKey(conceded.Number, KeyKinds.Empty));
        }

        if (!home.Keys.Any(key => key.Special == SpecialKey.GuestLogo) && Place(deck, home, GuestPlace, home: true) is { } guest)
            home = home.With(DefaultDecks.GuestLogoKey(guest));

        if (!home.Keys.Any(key => DefaultDecks.TeamLogoPage.Equals(key.PageId, StringComparison.OrdinalIgnoreCase))
            && Place(deck, home, PagePlace, home: true) is { } entry)
            home = home.With(DefaultDecks.TeamLogoKey(entry));

        return home == before ? null : home;
    }

    private static Func<DeckKey, bool> Shows(string value) =>
        key => key.Kind == KeyKinds.Live && value.Equals(key.Live, StringComparison.OrdinalIgnoreCase);

    private static int? Place(Deck deck, DeckPage page, int wanted, bool home = false)
    {
        if (wanted != deck.Back && Free(deck, page, wanted)) return wanted;
        for (var number = 1; number <= deck.Count; number++)
            if (number != deck.Back && Free(deck, page, number)) return number;
        return home && Free(deck, page, deck.Back) ? deck.Back : null;
    }

    private static bool Free(Deck deck, DeckPage page, int number) =>
        number != deck.Panic && page.KeyAt(number).IsEmpty;
}
