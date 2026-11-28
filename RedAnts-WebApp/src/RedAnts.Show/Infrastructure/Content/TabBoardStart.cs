using System.Text.Json;
using System.Text.RegularExpressions;
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

    public static class Categories
    {
        public const string Before = "Vor dem Spiel";
        public const string Break = "Pause";
        public const string After = "Nach dem Spiel";
        public const string Special = "Spezielle Situationen";
        public const string Music = "Musik";
        public const string Players = "Spielerinnen";
        public const string MusicControl = "Musiksteuerung";
        public const string Vmix = "vMix";
        public const string Led = "LED";
        public const string TeamLogos = "Teamlogos";
        public const string Messages = "Meldungen";
        public const string Live = "Anzeige";
        public const string Functions = "Funktionen";
    }

    private sealed record Source(DeckKey Key, string Label, string? League, string Category, ButtonAction Action);

    private static IEnumerable<BoardButton> ButtonsFrom(ShowSetup show)
    {
        var sources = show.DeckLibrary
            .SelectMany(deck => show.PagesOf(deck))
            .SelectMany(page => page.Keys.Select(key => (page, key)))
            .Where(pair => ButtonAction.Kinds.Contains(pair.key.Kind))
            .Where(pair => !(pair.key.Kind == KeyKinds.Special && Transport.Contains(pair.key.Special)))
            .Where(pair => !(pair.key.Kind == KeyKinds.Live && string.IsNullOrWhiteSpace(pair.key.Live)))
            .Select(pair => new Source(
                pair.key,
                CleanLabel(pair.key),
                LeagueOf(pair.page.Name),
                CategoryOf(pair.page, pair.key),
                new ButtonAction(pair.key.Kind, pair.key.MusicId, pair.key.Special, pair.key.Live, pair.key.Steps, pair.key.Livestream, pair.key.Led)))
            .Where(source => Valid(source.Action, source.Label))
            .ToList();

        var groups = sources
            .GroupBy(source => source.Key.Kind == KeyKinds.Music
                ? source.Label.ToLowerInvariant() + "|music"
                : source.Label.ToLowerInvariant() + "|" + Signature(source.Action))
            .ToList();
        var collisions = groups.GroupBy(g => g.First().Label, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).SelectMany(g => g).Select(g => g.Key).ToHashSet();
        var ids = new HashSet<string>(Transport, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var main = group.FirstOrDefault(s => s.League == Lupl) ?? group.First();
            var label = collisions.Contains(group.Key) ? Distinct(main) : main.Label;
            var leagues = group.Where(s => s.League is not null).Select(s => s.League!).Distinct().ToList();
            var everywhere = group.Any(s => s.League is null) || Leagues.All(l => leagues.Contains(l.Id));
            var variants = group
                .Where(s => s.League is not null && s.League != main.League && Signature(s.Action) != Signature(main.Action))
                .GroupBy(s => s.League!)
                .ToDictionary(g => g.Key, g => g.First().Action);
            yield return new BoardButton(
                UniqueId(Slug(label), ids),
                label,
                main.Key.Color,
                main.Action,
                main.Key.Icon is { Length: > 0 } icon ? icon : null,
                Leagues: everywhere ? null : leagues,
                PerLeague: variants.Count > 0 ? variants : null,
                Category: main.Category);
        }
    }

    private static string Signature(ButtonAction action) => JsonSerializer.Serialize(action, ShowJson.Options);

    private static string Distinct(Source source)
    {
        if (source.Key.Kind == KeyKinds.Special && SpecialKey.Of(source.Key.Special) is { } special) return special.Label;
        var steps = Signature(source.Action);
        if (steps.Contains("home", StringComparison.OrdinalIgnoreCase)) return source.Label + " Heim";
        if (steps.Contains("away", StringComparison.OrdinalIgnoreCase)) return source.Label + " Gast";
        return source.Label;
    }

    public static string CleanLabel(DeckKey key)
    {
        var label = key.Label is { Length: > 0 } text ? text : SpecialKey.Of(key.Special)?.Label ?? KeyKinds.Label(key.Kind);
        label = Regex.Replace(label, @"^\d+\.\s+", "");
        label = Regex.Replace(label, @"\s*\((?:nur\s+)?(?:L-UPL|U21|U17|1\.?\s*Liga)[^)]*\)", "", RegexOptions.IgnoreCase);
        label = Regex.Replace(label, @"\s+L-UPL$", "", RegexOptions.IgnoreCase);
        return label.Trim();
    }

    public static string? LeagueOf(string page)
    {
        if (page.Contains("L-UPL", StringComparison.OrdinalIgnoreCase)) return Lupl;
        if (page.Contains("U21", StringComparison.OrdinalIgnoreCase)) return U21;
        if (page.Contains("U17", StringComparison.OrdinalIgnoreCase)) return U17;
        if (Regex.IsMatch(page, @"1\.?\s*Liga", RegexOptions.IgnoreCase)) return FirstLeague;
        return null;
    }

    private static string CategoryOf(DeckPage page, DeckKey key)
    {
        if (key.Kind == KeyKinds.Live) return Categories.Live;
        if (key.Kind == KeyKinds.Special)
        {
            if (SpecialKey.ChoosesPlayer(key.Special) || key.Special is { } id && (id.StartsWith("spieler-") || id.StartsWith("bestplayer-") || id.StartsWith("starting6-")))
                return Categories.Players;
            if (key.Special is { } music && (music.StartsWith("musik-") || music == SpecialKey.AutoBreakMusic)) return Categories.MusicControl;
            return Categories.Functions;
        }
        var name = page.Name;
        if (name.Contains("Teamlogo", StringComparison.OrdinalIgnoreCase)) return Categories.TeamLogos;
        if (name.Contains("Meldung", StringComparison.OrdinalIgnoreCase)) return Categories.Messages;
        if (key.Kind == KeyKinds.Music)
        {
            if (name.StartsWith("Vor", StringComparison.OrdinalIgnoreCase)) return Categories.Before;
            if (name.StartsWith("Pause", StringComparison.OrdinalIgnoreCase)) return Categories.Break;
            if (name.StartsWith("Nach", StringComparison.OrdinalIgnoreCase)) return Categories.After;
            if (name.Contains("Special", StringComparison.OrdinalIgnoreCase)) return Categories.Special;
            return Categories.Music;
        }
        var steps = key.Steps ?? [];
        if (steps.Count > 0 && steps.All(s => s.Action.StartsWith("led.") || s.Action == KeyActions.Wait)) return Categories.Led;
        if (steps.Any(s => s.Action.StartsWith("vmix."))) return Categories.Vmix;
        return Categories.Functions;
    }

    private static bool Valid(ButtonAction action, string label)
    {
        try { action.Validated(label); return true; }
        catch (ValidationException) { return false; }
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

    public static string Slug(string label) => ButtonIds.Slug(label);

    private static string UniqueId(string slug, HashSet<string> taken)
    {
        var id = ButtonIds.Next(slug, taken);
        taken.Add(id);
        return id;
    }
}
