using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;
using RedAnts.Show.Features.Music;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace RedAnts.Show.Infrastructure.Content;

public sealed class ShowExampleSeeder(
    IHallRepository halls,
    IMusicLibrary music,
    ILogger<ShowExampleSeeder> log) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public const string HallId = "win4";
    public const string HallName = "WIN4 Stratos";
    public const string MusicHallId = "oberseen";
    public const string MusicHallName = "Oberseen";

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken stop)
    {
        try
        {
            var library = await music.TreeAsync();
            if (await halls.GetByIdAsync(HallId) is null)
            {
                var hall = Example(library);
                await halls.SaveAsync(hall);
                log.LogInformation("Beispielhalle {Hall} angelegt: {Decks} Decks", hall.Name, hall.DeckLayouts.Count);
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

    public static Hall Example(IReadOnlyList<MusicNode> library)
    {
        var hall = Hall.Create(HallId, HallName) with
        {
            Devices = new HallDevices(VmixHost: "127.0.0.1", SednaHost: "127.0.0.1", ClockType: "iCast", ClockPort: 50086),
            Offered = new HallCapabilities(Sound: true, Clock: true, Vmix: true, Led: true),
            BreakMusicId = Find(library, "Spielunterbruch")?.Id ?? "",
        };
        return hall with { Decks = [Field(library), Music(library, DefaultDecks.Music())] };
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

    private static Deck Field(IReadOnlyList<MusicNode> library)
    {
        var deck = DefaultDecks.Field();
        var home = deck.Home;
        home = WithMusic(home, 1, Find(library, "Tor Red Ants"));
        home = WithMusic(home, 8, Find(library, "Tor Gegner"));
        home = WithMusic(home, 9, Find(library, "Strafe Red Ants"));
        home = WithMusic(home, 16, Find(library, "Strafe Gegner"));
        return deck with { Pages = deck.Pages.Select(p => p.IsHome ? home : p).ToList() };
    }

    private static Deck Music(IReadOnlyList<MusicNode> library, Deck deck)
    {
        var transport = deck.Home.Keys.Where(k => !deck.Reserved(k.Number)).ToList();
        var pages = new List<DeckPage>();
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var free = Free(deck, transport.Select(k => k.Number)).ToList();

        pages.Add(Page(DeckPage.HomeId, "Startseite", null, library, free, deck, pages, taken, transport));
        return deck with { Pages = pages };
    }

    private static DeckPage Page(
        string id,
        string name,
        string? parent,
        IReadOnlyList<MusicNode> nodes,
        IReadOnlyList<int> free,
        Deck deck,
        List<DeckPage> pages,
        HashSet<string> taken,
        IReadOnlyList<DeckKey> transport)
    {
        var keys = new List<DeckKey>(id == DeckPage.HomeId ? transport : []);
        var at = 0;
        foreach (var node in nodes)
        {
            if (at >= free.Count) break;
            var number = free[at++];
            if (node.IsFolder)
            {
                var pageId = PageId(node, taken);
                keys.Add(new DeckKey(number, KeyKinds.Page, node.Name, Colour(node), Symbol(node), PageId: pageId));
                var below = Free(deck, []).ToList();
                pages.Add(Page(pageId, node.Name, id, node.Children, below, deck, pages, taken, transport));
                continue;
            }
            keys.Add(new DeckKey(number, KeyKinds.Music, node.Name, Colour(node), Symbol(node), MusicId: node.Id));
        }
        return new DeckPage(id, name, parent, keys.OrderBy(k => k.Number).ToList());
    }

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
