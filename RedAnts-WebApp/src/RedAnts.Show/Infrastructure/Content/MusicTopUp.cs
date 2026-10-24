using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Infrastructure.Content;

public static class MusicTopUp
{
    public const string SettingKey = "Show:DeckMusicTopUp";
    public const string Version = "1-liga-timeout-penalty";

    public const string OverviewPage = "vor-nach-spiel";
    public const string SpecialPage = "special";
    public const string LeaguePage = "1liga";
    public const string BeforePage = "vor-1liga";
    public const string BreakPage = "pause-1liga";
    public const string AfterPage = "nach-1liga";

    private const string League = "Vor dem Spiel 1. Liga";
    private const string LeagueLabel = "1. Liga";
    private const string DeadColor = "#000000";
    private const int TitlePlace = 5;

    private static readonly string[] Specials = ["Time Out", "Penalty"];

    public static Deck? Fill(Deck deck, IReadOnlyList<MusicItem> library)
    {
        var pages = deck.Pages.ToList();
        var placed = Placed(deck);
        var changed = AddSpecials(deck, pages, library, placed);
        changed |= AddLeague(deck, pages, library, placed);
        return changed ? deck with { Pages = pages } : null;
    }

    private static bool AddSpecials(Deck deck, List<DeckPage> pages, IReadOnlyList<MusicItem> library, ISet<string> placed)
    {
        var missing = Specials
            .Select(name => library.FirstOrDefault(item => Leaf(item).Equals(name, StringComparison.OrdinalIgnoreCase)))
            .OfType<MusicItem>()
            .Where(item => !placed.Contains(item.Id))
            .ToList();
        if (missing.Count == 0) return false;

        var page = Ensure(deck, pages, SpecialPage, "Special Situations", DeckPage.HomeId);
        foreach (var item in missing)
            if (FreePlace(deck, page) is { } place) page = page.With(Song(place, item));
        Replace(pages, page);
        return true;
    }

    private static bool AddLeague(Deck deck, List<DeckPage> pages, IReadOnlyList<MusicItem> library, ISet<string> placed)
    {
        var songs = library
            .Where(item => Group(item).Contains(League, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => Leaf(item), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (songs.Count == 0 || songs.All(item => placed.Contains(item.Id))) return false;

        var colour = songs[0].Color is { Length: > 0 } known ? known : DeckKey.DefaultColor;
        var overview = pages.FirstOrDefault(p => p.Id == OverviewPage) is not null ? OverviewPage : DeckPage.HomeId;
        var league = Ensure(deck, pages, LeaguePage, LeagueLabel, overview);
        Fill(deck, pages, BeforePage, $"Vor Spiel {LeagueLabel}", LeaguePage, songs);
        Fill(deck, pages, BreakPage, $"Pause {LeagueLabel}", LeaguePage, Shared(library, "Pause"));
        Fill(deck, pages, AfterPage, $"Nach Spiel {LeagueLabel}", LeaguePage,
            [.. Shared(library, "Nach Spiel (Sieg"), .. Shared(library, "Nach dem Spiel (Niederlage")]);

        foreach (var (id, label) in new[]
                 {
                     (BeforePage, $"Vor Spiel {LeagueLabel}"),
                     (BreakPage, $"Pause {LeagueLabel}"),
                     (AfterPage, $"Nach Spiel {LeagueLabel}"),
                 })
        {
            if (league.Keys.Any(key => key.PageId == id)) continue;
            if (FreePlace(deck, league) is not { } place) continue;
            league = league.With(new DeckKey(place, KeyKinds.Page, label, colour, PageId: id));
        }
        Replace(pages, league);

        var parent = pages.First(p => p.Id == overview);
        if (parent.Keys.All(key => key.PageId != LeaguePage) && FreePlace(deck, parent) is { } spot)
            Replace(pages, parent.With(new DeckKey(spot, KeyKinds.Page, LeagueLabel, colour, "📁", PageId: LeaguePage)));
        return true;
    }

    private static void Fill(Deck deck, List<DeckPage> pages, string id, string name, string parent, IReadOnlyList<MusicItem> songs)
    {
        if (songs.Count == 0 || pages.Any(p => p.Id == id)) return;
        var page = Ensure(deck, pages, id, name, parent);
        foreach (var item in songs)
            if (FreePlace(deck, page) is { } place) page = page.With(Song(place, item));
        Replace(pages, page);
    }

    private static DeckPage Ensure(Deck deck, List<DeckPage> pages, string id, string name, string parent)
    {
        if (pages.FirstOrDefault(p => p.Id == id) is { } known) return known;
        var page = new DeckPage(id, name, parent, [Dead(name), .. DefaultDecks.Transport()]);
        pages.Add(page);
        return page;
    }

    private static void Replace(List<DeckPage> pages, DeckPage page)
    {
        var index = pages.FindIndex(p => p.Id == page.Id);
        if (index < 0) pages.Add(page);
        else pages[index] = page;
    }

    private static IReadOnlyList<MusicItem> Shared(IReadOnlyList<MusicItem> library, string name) =>
        library.Where(item =>
                Leaf(item).StartsWith(name, StringComparison.OrdinalIgnoreCase)
                && !Group(item).Contains("Vor dem Spiel", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => Group(item).Length)
            .Take(1)
            .ToList();

    private static int? FreePlace(Deck deck, DeckPage page)
    {
        foreach (var number in Places(deck))
            if (page.KeyAt(number).IsEmpty) return number;
        return null;
    }

    private static IEnumerable<int> Places(Deck deck)
    {
        var transport = DefaultDecks.Transport().Select(key => key.Number).ToHashSet();
        for (var number = 1; number <= deck.Count; number++)
            if (number != deck.Back && number != deck.Panic && number != TitlePlace && !transport.Contains(number))
                yield return number;
    }

    private static HashSet<string> Placed(Deck deck)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in deck.Pages.SelectMany(page => page.Keys))
        {
            if (key.MusicId is { Length: > 0 } id) used.Add(id);
            foreach (var step in key.Sequence)
                if (step.Action == KeyActions.MusicPlay && step.Target is { Length: > 0 } target) used.Add(target);
        }
        return used;
    }

    private static DeckKey Song(int number, MusicItem item) =>
        new(number, KeyKinds.Music, Leaf(item),
            item.Color is { Length: > 0 } color ? color : DeckKey.DefaultColor,
            item.Icon is { Length: > 0 } icon ? icon : null,
            MusicId: item.Id);

    private static DeckKey Dead(string name) =>
        new(TitlePlace, KeyKinds.Live, name, DeadColor, Live: "");

    private static string Group(MusicItem item)
    {
        var cut = item.Name.LastIndexOf('·');
        return cut < 0 ? "" : item.Name[..cut].Trim();
    }

    private static string Leaf(MusicItem item)
    {
        var cut = item.Name.LastIndexOf('·');
        return cut < 0 ? item.Name.Trim() : item.Name[(cut + 1)..].Trim();
    }
}
