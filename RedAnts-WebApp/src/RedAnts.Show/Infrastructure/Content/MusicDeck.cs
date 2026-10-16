using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Infrastructure.Content;

public static class MusicDeck
{
    public const string BeforeAndAfter = "vor-nach-spiel";
    public const string Special = "special";

    private const string Folder = "Vor/Nach Spiel / Pause";
    private const string Situations = "Special Situations";
    private const string PageColor = "#2b3038";
    private const string DeadColor = "#000000";

    private sealed record League(string Key, string Name, string Folder, bool Sponsors);

    private static readonly IReadOnlyList<League> Leagues =
    [
        new("lupl", "L-UPL", "Vor dem Spiel L-UPL", true),
        new("u21", "U21", "Vor dem Spiel U21", false),
        new("u17", "U17", "Vor dem Spiel U17", false),
    ];

    public static Deck Build(Deck deck, IReadOnlyList<MusicNode> library)
    {
        var pages = new List<DeckPage> { Start(library), Overview(library) };
        foreach (var league in Leagues)
        {
            pages.Add(Before(league, library));
            pages.Add(Break(league, library));
            pages.Add(After(league, library));
        }
        pages.Add(Situation(library));
        return deck with { Pages = pages };
    }

    private static DeckPage Start(IReadOnlyList<MusicNode> library) =>
        new(DeckPage.HomeId, "Startseite", null,
        [
            Opens(1, BeforeAndAfter, "Vor/Nach Spiel / Pause", Find(library, Folder)),
            Opens(2, Special, Situations, Find(library, Situations)),
            Song(5, Find(library, "Spielunterbruch")),
            Automatic(10),
            .. DefaultDecks.Transport(),
        ]);

    private static DeckPage Overview(IReadOnlyList<MusicNode> library)
    {
        var keys = new List<DeckKey> { Dead(5, "Vor/Nach Spiel, Pause"), Song(10, Find(library, Folder, "Aufwärmmusik")) };
        var places = new[] { 2, 7, 12 };
        for (var i = 0; i < Leagues.Count; i++)
        {
            var league = Leagues[i];
            var colour = Colour(Find(library, Folder, league.Folder));
            keys.Add(new DeckKey(places[i], KeyKinds.Page, $"Vor Spiel {league.Name}", colour, "📣", PageId: Before(league)));
            keys.Add(new DeckKey(places[i] + 1, KeyKinds.Page, $"Pause {league.Name}", colour, "⏸", PageId: Break(league)));
            keys.Add(new DeckKey(places[i] + 2, KeyKinds.Page, $"Nach Spiel {league.Name}", colour, "🏁", PageId: After(league)));
        }
        return new DeckPage(BeforeAndAfter, "Vor/Nach Spiel / Pause", DeckPage.HomeId, keys.OrderBy(k => k.Number).ToList());
    }

    private static DeckPage Before(League league, IReadOnlyList<MusicNode> library)
    {
        var songs = Inside(library, Folder, league.Folder);
        return Flow(Before(league), $"Vor Spiel {league.Name}", songs);
    }

    private static DeckPage Break(League league, IReadOnlyList<MusicNode> library)
    {
        var songs = new List<MusicNode?>();
        if (league.Sponsors) songs.Add(Find(library, Folder, "Lidl Pausendurchsage"));
        songs.Add(Find(library, Folder, "Pause"));
        return Flow(Break(league), $"Pause {league.Name}", songs);
    }

    private static DeckPage After(League league, IReadOnlyList<MusicNode> library)
    {
        var songs = new List<MusicNode?>();
        if (league.Sponsors) songs.Add(Find(library, Folder, "Lidl nach Spiel"));
        songs.Add(Find(library, Folder, "Nach Spiel (Sieg"));
        songs.Add(Find(library, Folder, "Nach dem Spiel (Niederlage"));
        return Flow(After(league), $"Nach Spiel {league.Name}", songs);
    }

    private static DeckPage Situation(IReadOnlyList<MusicNode> library) =>
        Flow(Special, Situations, Inside(library, Situations), DeckPage.HomeId);

    private static DeckPage Flow(string id, string name, IReadOnlyList<MusicNode?> songs, string parent = BeforeAndAfter)
    {
        var places = new[] { 2, 3, 4, 6, 7, 8, 9, 10 };
        var keys = new List<DeckKey> { Dead(5, name) };
        keys.AddRange(DefaultDecks.Transport());
        for (var i = 0; i < songs.Count && i < places.Length; i++)
            if (songs[i] is { } song) keys.Add(Song(places[i], song));
        return new DeckPage(id, name, parent, keys.OrderBy(k => k.Number).ToList());
    }

    private static string Before(League league) => "vor-" + league.Key;

    private static string Break(League league) => "pause-" + league.Key;

    private static string After(League league) => "nach-" + league.Key;

    private static DeckKey Opens(int number, string pageId, string label, MusicNode? node) =>
        new(number, KeyKinds.Page, label, Colour(node), Symbol(node) ?? "📁", PageId: pageId);

    private static DeckKey Song(int number, MusicNode? node) =>
        node is null
            ? new DeckKey(number, KeyKinds.Empty)
            : new DeckKey(number, KeyKinds.Music, node.Name, Colour(node), Symbol(node), MusicId: node.Id);

    private static DeckKey Dead(int number, string label) =>
        new(number, KeyKinds.Live, label, DeadColor, Live: "");

    private static DeckKey Automatic(int number) =>
        new(number, KeyKinds.Special, "Spielunterbruch automatisch", "#1e7b34", "🔁", Special: SpecialKey.AutoBreakMusic);

    private static string Colour(MusicNode? node) =>
        node?.Color is { Length: > 0 } colour ? colour : PageColor;

    private static string? Symbol(MusicNode? node) =>
        node?.Icon is { Length: > 0 } icon ? icon : null;

    private static IReadOnlyList<MusicNode?> Inside(IReadOnlyList<MusicNode> library, params string[] path)
    {
        var nodes = Walk(library, path);
        return nodes is null ? [] : nodes.Where(n => !n.IsFolder && n.Control.Length == 0).Cast<MusicNode?>().ToList();
    }

    private static MusicNode? Find(IReadOnlyList<MusicNode> library, params string[] path)
    {
        if (path.Length == 0) return null;
        var nodes = path.Length == 1 ? library : Walk(library, path[..^1]);
        return nodes?.FirstOrDefault(n => n.Name.Contains(path[^1], StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<MusicNode>? Walk(IReadOnlyList<MusicNode> library, IReadOnlyList<string> path)
    {
        var nodes = library;
        foreach (var step in path)
        {
            var folder = nodes.FirstOrDefault(n => n.IsFolder && n.Name.Contains(step, StringComparison.OrdinalIgnoreCase));
            if (folder is null) return null;
            nodes = folder.Children;
        }
        return nodes;
    }
}
