using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace RedAnts.Show.Infrastructure.Content;

public sealed class ShowExampleSeeder(
    IHallRepository halls,
    IMusicLibrary music,
    AgentSync sync,
    ILogger<ShowExampleSeeder> log) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string HallId = "win4";
    public const string HallName = "WIN4 Stratos";
    public const string MusicHallId = "oberseen";
    public const string MusicHallName = "Oberseen";

    private static readonly int[] TimeoutKeys = [3, 6];

    private const int HighlightKey = 22;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken stop)
    {
        try
        {
            var library = await music.TreeAsync();
            if (await halls.GetByIdAsync(HallId) is not { } known)
            {
                var hall = Example(library);
                await halls.SaveAsync(hall);
                log.LogInformation("Beispielhalle {Hall} angelegt: {Decks} Decks", hall.Name, hall.DeckLayouts.Count);
            }
            else if (Refreshed(known, library) is { } updated)
            {
                await halls.SaveAsync(updated);
                await sync.PushAsync(updated.Id);
                log.LogInformation("Decks der Halle {Hall} nachgeführt", updated.Name);
            }
            if (await halls.GetByIdAsync(MusicHallId) is null)
            {
                var hall = MusicHall(library);
                await halls.SaveAsync(hall);
                log.LogInformation("Musikhalle {Hall} angelegt", hall.Name);
            }
        }
        catch (Exception e)
        {
            log.LogWarning("Beispielhalle konnte nicht angelegt werden: {Message}", e.Message);
        }
    }

    public static Hall? Refreshed(Hall hall, IReadOnlyList<MusicNode> library)
    {
        var decks = hall.DeckLayouts.ToList();
        var changed = false;

        if (decks.FirstOrDefault(d => d.Id == DefaultDecks.MusicDeck) is { } music && music.Page(MusicDeck.BeforeAndAfter) is null)
        {
            decks[decks.IndexOf(music)] = MusicDeck.Build(DefaultDecks.Music() with { Name = music.Name }, library);
            changed = true;
        }

        if (decks.FirstOrDefault(d => d.Id == DefaultDecks.FieldDeck) is { } field)
        {
            var home = field.Home;
            var fresh = home;
            if (Find(library, "Time Out") is { } timeout)
                foreach (var number in TimeoutKeys)
                    if (!fresh.KeyAt(number).Sequence.Any(step => step.Action == KeyActions.MusicPlay))
                        fresh = WithMusic(fresh, number, timeout);

            var wanted = DefaultDecks.Field().Home.KeyAt(HighlightKey);
            if (fresh.KeyAt(HighlightKey).Sequence.Any(step => step.Target == "title:result"))
                fresh = fresh.With(wanted);

            foreach (var key in fresh.Keys.Where(Timed).ToList())
                fresh = fresh.With(key with { Steps = key.Sequence.Where(Shows).ToList() });

            if (fresh != home)
            {
                decks[decks.IndexOf(field)] = field with { Pages = field.Pages.Select(p => p.IsHome ? fresh : p).ToList() };
                changed = true;
            }
        }

        var titles = hall.OverlayTitles.ToList();
        foreach (var title in Hall.DefaultOverlayTitles)
        {
            var index = titles.FindIndex(t => t.Kind.Equals(title.Kind, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                titles.Add(title);
                changed = true;
                continue;
            }
            if (!Clipped(titles[index])) continue;
            titles[index] = title;
            changed = true;
        }

        return changed ? hall with { Decks = decks, OverlayTitles = titles } : null;
    }

    public static Hall Example(IReadOnlyList<MusicNode> library)
    {
        var hall = Hall.Create(HallId, HallName) with
        {
            Devices = new HallDevices(VmixHost: "127.0.0.1", SednaHost: "127.0.0.1", ClockType: "iCast", ClockPort: 50086),
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: true, Led: true),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [Field(library), MusicDeck.Build(DefaultDecks.Music(), library)] };
    }

    public static Hall MusicHall(IReadOnlyList<MusicNode> library)
    {
        var hall = Hall.Create(MusicHallId, MusicHallName) with
        {
            Offered = new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: false),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [Music(library, DefaultDecks.Music() with { Id = "bord", Name = "Bord", Columns = 5, Rows = 4, PanicKey = 20, BackKey = 16 })] };
    }

    private static bool Timed(DeckKey key) =>
        key.Sequence.Any(step => step.Action == KeyActions.VmixShow)
        && key.Sequence.Any(step => step.Action == KeyActions.Wait);

    private static bool Shows(KeyStep step) =>
        step.Action is not (KeyActions.Wait or KeyActions.VmixHide);

    private static bool Clipped(OverlayTitle title) =>
        title.Fields.TryGetValue("TxtBottom.Text", out var bottom)
        && (bottom.Contains("referee2", StringComparison.OrdinalIgnoreCase)
            || bottom.Contains("commentator2", StringComparison.OrdinalIgnoreCase));

    private static Deck Field(IReadOnlyList<MusicNode> library)
    {
        var deck = DefaultDecks.Field();
        var home = deck.Home;
        home = WithMusic(home, 1, Find(library, "Tor Red Ants"));
        home = WithMusic(home, 8, Find(library, "Tor Gegner"));
        home = WithMusic(home, 9, Find(library, "Strafe Red Ants"));
        home = WithMusic(home, 16, Find(library, "Strafe Gegner"));
        foreach (var number in TimeoutKeys) home = WithMusic(home, number, Find(library, "Time Out"));
        return deck with { Pages = deck.Pages.Select(p => p.IsHome ? home : p).ToList() };
    }

    private static Deck Music(IReadOnlyList<MusicNode> library, Deck deck)
    {
        var controls = library.Any(n => n.Control.Length > 0);
        var extra = controls
            ? deck.Home.Keys.Where(k => k.Special is SpecialKey.AutoBreakMusic or SpecialKey.MusicStop).ToList()
            : deck.Home.Keys.Where(k => !deck.Reserved(k.Number)).ToList();
        var pages = new List<DeckPage>();
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        pages.Insert(0, Page(DeckPage.HomeId, "Startseite", null, library, deck, pages, taken, extra));
        return deck with { Pages = pages };
    }

    private static DeckPage Page(
        string id,
        string name,
        string? parent,
        IReadOnlyList<MusicNode> nodes,
        Deck deck,
        List<DeckPage> pages,
        HashSet<string> taken,
        IReadOnlyList<DeckKey> extra)
    {
        var keys = new List<DeckKey>();
        var used = new HashSet<int>();

        foreach (var node in nodes)
            if (Placed(node, deck, used) is { } number)
            {
                used.Add(number);
                keys.Add(KeyFor(node, number, id, deck, pages, taken, extra));
            }

        var free = Free(deck, used).ToList();
        var at = 0;
        foreach (var node in nodes)
        {
            if (Placed(node, deck, used) is not null || PlacedAt(node, deck) is not null) continue;
            if (at >= free.Count) break;
            var number = free[at++];
            used.Add(number);
            keys.Add(KeyFor(node, number, id, deck, pages, taken, extra));
        }

        if (id == DeckPage.HomeId)
            foreach (var key in extra)
            {
                while (at < free.Count && used.Contains(free[at])) at++;
                if (at >= free.Count) break;
                var number = free[at++];
                used.Add(number);
                keys.Add(key with { Number = number });
            }

        return new DeckPage(id, name, parent, keys.OrderBy(k => k.Number).ToList());
    }

    private static DeckKey KeyFor(
        MusicNode node,
        int number,
        string pageId,
        Deck deck,
        List<DeckPage> pages,
        HashSet<string> taken,
        IReadOnlyList<DeckKey> extra)
    {
        if (node.Control is { Length: > 0 } control && SpecialOf(control) is { } special)
            return new DeckKey(number, KeyKinds.Special, node.Name, Colour(node),
                SpecialKey.Of(special)?.Icon is { Length: > 0 } icon ? icon : Symbol(node), Special: special);

        if (node.IsFolder)
        {
            var id = PageId(node, taken);
            pages.Add(Page(id, node.Name, pageId, node.Children, deck, pages, taken, extra));
            return new DeckKey(number, KeyKinds.Page, node.Name, Colour(node), Symbol(node), PageId: id);
        }

        return new DeckKey(number, KeyKinds.Music, node.Name, Colour(node), Symbol(node), MusicId: node.Id);
    }

    private static int? PlacedAt(MusicNode node, Deck deck)
    {
        if (node.X < 0 || node.Y < 0 || node.X >= deck.Columns) return null;
        var number = node.Y * deck.Columns + node.X + 1;
        return number >= 1 && number <= deck.Count && !deck.Reserved(number) ? number : null;
    }

    private static string? SpecialOf(string control) => control switch
    {
        "pause" => SpecialKey.MusicPause,
        "fade" => SpecialKey.MusicFade,
        "previous" => SpecialKey.MusicPrevious,
        "next" => SpecialKey.MusicNext,
        _ => null,
    };

    private static int? Placed(MusicNode node, Deck deck, HashSet<int> used) =>
        PlacedAt(node, deck) is { } number && !used.Contains(number) ? number : null;

    private static IEnumerable<int> Free(Deck deck, IEnumerable<int> used)
    {
        var blocked = used.ToHashSet();
        for (var number = 1; number <= deck.Count; number++)
        {
            if (deck.Reserved(number) || blocked.Contains(number)) continue;
            yield return number;
        }
    }

    private static string PageId(MusicNode node, HashSet<string> taken)
    {
        var id = Key(node.Name);
        while (!taken.Add(id)) id += "-2";
        return id;
    }

    private static string Key(string name)
    {
        var slug = new string(name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-");
        return slug.Length > 0 ? slug[..Math.Min(30, slug.Length)] : "seite";
    }

    private static DeckPage WithMusic(DeckPage page, int number, MusicNode? item)
    {
        if (item is null) return page;
        var key = page.KeyAt(number);
        var steps = new List<KeyStep> { new(KeyActions.MusicPlay, item.Id) };
        steps.AddRange(key.Sequence.Where(s => s.Action != KeyActions.MusicPlay));
        return page.With(key with { Steps = steps });
    }

    private static string Colour(MusicNode node) =>
        node.Color is { Length: > 0 } colour ? colour : DeckKey.DefaultColor;

    private static string? Symbol(MusicNode node) =>
        node.Icon is { Length: > 0 } icon ? icon : null;

    private static MusicNode? Find(IReadOnlyList<MusicNode> nodes, string label)
    {
        foreach (var node in nodes)
        {
            if (!node.IsFolder && node.Name.Contains(label, StringComparison.OrdinalIgnoreCase)) return node;
            if (node.IsFolder && Find(node.Children, label) is { } found) return found;
        }
        return null;
    }
}
