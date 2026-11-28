using System.Text;
using System.Text.Json;
using RedAnts.Show.Domain;

namespace RedAnts.Show.Infrastructure.Content;

public static class TabBoardStart
{
    public const string Win4 = "win4";
    public const string Oberseen = "oberseen";
    public const string Rennweg = "rennweg";

    public const string Lupl = "lupl";
    public const string U21 = "u21";
    public const string U17 = "u17";
    public const string FirstLeague = "liga1";

    public const string XlDeck = "tabs-xl";
    public const string Mk2Deck = "tabs-mk2";

    private static readonly string[] Transport =
        [SpecialKey.MusicPrevious, SpecialKey.MusicPause, SpecialKey.MusicNext, SpecialKey.MusicStop];

    public static readonly IReadOnlyList<(string Id, string[] Halls)> VenueSources =
    [
        (Win4, ["win4"]),
        (Oberseen, ["oberseen-notebook", "oberseen"]),
        (Rennweg, ["rennweg-ipad"]),
    ];

    public static TabBoard Build(ShowSetup show, IReadOnlyDictionary<string, Hall> halls)
    {
        var buttons = TransportButtons().Concat(ButtonsFrom(show)).ToList();
        return new TabBoard(Venues(halls), Leagues, buttons, [], Decks(show));
    }

    public static IReadOnlyList<League> Leagues { get; } =
    [
        new(Lupl, "L-UPL", Livestream: true, Players: true, Screen: "spiel-nla"),
        new(U21, "U21", Livestream: false, Players: false, Screen: "spiel-u21"),
        new(U17, "U17", Livestream: false, Players: false, Screen: "spiel-u21"),
        new(FirstLeague, "1. Liga", Livestream: false, Players: false),
    ];

    private static IReadOnlyList<Venue> Venues(IReadOnlyDictionary<string, Hall> halls) =>
        VenueSources.Select(source =>
        {
            var hall = source.Halls.Select(id => halls.GetValueOrDefault(id)).OfType<Hall>().FirstOrDefault();
            return new Venue(
                source.Id,
                Name(source.Id),
                hall?.Devices ?? new HallDevices(),
                hall?.Offers ?? new HallCapabilities(Sound: true, Clock: false, Vmix: false, Led: false));
        }).ToList();

    private static string Name(string venue) => venue switch
    {
        Win4 => "WIN4",
        Oberseen => "Oberseen",
        Rennweg => "Rennweg",
        _ => venue,
    };

    private static IEnumerable<BoardButton> TransportButtons() =>
        Transport.Select(id => SpecialKey.Of(id)!).Select(special => new BoardButton(
            special.Id,
            special.Label,
            DeckKey.DefaultColor,
            new ButtonAction(KeyKinds.Special, Special: special.Id),
            special.Icon is { Length: > 0 } icon ? icon : null));

    private static IEnumerable<BoardButton> ButtonsFrom(ShowSetup show)
    {
        var seen = new HashSet<string>();
        var ids = new HashSet<string>(Transport, StringComparer.OrdinalIgnoreCase);
        var keys = show.DeckLibrary
            .SelectMany(deck => show.PagesOf(deck))
            .SelectMany(page => page.Keys)
            .Where(key => ButtonAction.Kinds.Contains(key.Kind))
            .Where(key => !(key.Kind == KeyKinds.Special && Transport.Contains(key.Special)));
        foreach (var key in keys)
        {
            var action = new ButtonAction(key.Kind, key.MusicId, key.Special, key.Live, key.Steps, key.Livestream, key.Led);
            var label = key.Label is { Length: > 0 } text ? text : SpecialKey.Of(key.Special)?.Label ?? KeyKinds.Label(key.Kind);
            var signature = label + "|" + key.Color + "|" + key.Icon + "|" + JsonSerializer.Serialize(action, ShowJson.Options);
            if (!seen.Add(signature)) continue;
            yield return new BoardButton(UniqueId(Slug(label), ids), label, key.Color, action, key.Icon);
        }
    }

    private static IReadOnlyList<BoardDeck> Decks(ShowSetup show)
    {
        var xl = show.DeckLibrary.FirstOrDefault(d => d.Columns == 8 && d.Rows == 4);
        var mk2 = show.DeckLibrary.FirstOrDefault(d => d.Columns == 5 && d.Rows == 3);
        var tabs = new BoardDeck(XlDeck, "Streamdeck XL (Tabs)", 8, 4, FollowsTab: true,
            PanicKey: xl?.PanicKey ?? 0, BackKey: xl?.BackKey ?? 0);
        tabs = tabs with { Fixed = TransportLayer(tabs) };
        var pult = new BoardDeck(Mk2Deck, "Streamdeck Mk2", 5, 3, FollowsTab: false, Own: BoardLayout.Empty,
            PanicKey: mk2?.PanicKey ?? 0, BackKey: mk2?.BackKey ?? 0);
        return [tabs, pult];
    }

    private static BoardLayout TransportLayer(BoardDeck deck)
    {
        var free = Enumerable.Range(1, deck.Count).Reverse().Where(n => !deck.Reserved(n)).Take(Transport.Length).Reverse().ToList();
        return new BoardLayout(Keys: Transport.Zip(free, (id, number) => new Placement(number, id)).ToList());
    }

    public static string Slug(string label)
    {
        var text = label.ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        var slug = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsAsciiLetterOrDigit(c)) slug.Append(c);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }
        var result = slug.ToString().Trim('-');
        return result.Length > 0 ? result : "button";
    }

    private static string UniqueId(string slug, HashSet<string> taken)
    {
        var id = slug;
        for (var n = 2; !taken.Add(id); n++) id = $"{slug}-{n}";
        return id;
    }
}
