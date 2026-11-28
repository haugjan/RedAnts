namespace RedAnts.Show.Domain;

public static class BoardTabs
{
    public const string Before = "vor-spiel";
    public const string During = "spiel";
    public const string Break = "pause";
    public const string After = "nach-spiel";
    public const string Other = "sonstiges";

    public static IReadOnlyList<(string Id, string Label)> All { get; } =
    [
        (Before, "Vor dem Spiel"),
        (During, "Während dem Spiel"),
        (Break, "Pause"),
        (After, "Nach dem Spiel"),
        (Other, "Sonstiges"),
    ];

    public static bool Known(string? id) => All.Any(tab => tab.Id == id);

    public static string Label(string id) => All.FirstOrDefault(tab => tab.Id == id).Label ?? id;
}

public sealed record Venue(string Id, string Name, HallDevices Devices, HallCapabilities Equipment)
{
    public Venue Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Halle: Kennung");
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Halle: Name");
        return this;
    }
}

public sealed record League(string Id, string Name, bool Livestream = true, bool Players = true, string Screen = "")
{
    public string ScreenOrDefault => Screen is { Length: > 0 } chosen ? chosen : Hall.FallbackScreen;

    public League Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Liga: Kennung");
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Liga: Name");
        return this;
    }
}

public sealed record ButtonAction(
    string Kind,
    string? MusicId = null,
    string? Special = null,
    string? Live = null,
    IReadOnlyList<KeyStep>? Steps = null,
    bool Livestream = true,
    bool Led = true)
{
    public static readonly IReadOnlyList<string> Kinds = [KeyKinds.Music, KeyKinds.Steps, KeyKinds.Special, KeyKinds.Live];

    public IReadOnlyList<KeyStep> Sequence => Steps ?? [];

    public ButtonAction Validated(string button)
    {
        if (!Kinds.Contains(Kind))
            throw new ValidationException(nameof(Kind), $"Button „{button}“: unbekannte Art „{Kind}“.");
        if (Kind == KeyKinds.Music && string.IsNullOrWhiteSpace(MusicId))
            throw new ValidationException(nameof(MusicId), $"Button „{button}“: bitte eine Musik wählen.");
        if (Kind == KeyKinds.Special && SpecialKey.Of(Special) is null)
            throw new ValidationException(nameof(Special), $"Button „{button}“: bitte eine feste Funktion wählen.");
        foreach (var step in Sequence) step.Validated();
        return this;
    }
}

public sealed record BoardButton(
    string Id,
    string Label,
    string Color,
    ButtonAction Action,
    string? Icon = null,
    IReadOnlyList<string>? Leagues = null,
    IReadOnlyDictionary<string, ButtonAction>? PerLeague = null,
    string? State = null,
    string? Category = null)
{
    public bool OfferedIn(string league) =>
        Leagues is not { Count: > 0 } only || only.Contains(league, StringComparer.OrdinalIgnoreCase);

    public ButtonAction ActionIn(string league) =>
        PerLeague is { Count: > 0 } variants
        && variants.FirstOrDefault(v => v.Key.Equals(league, StringComparison.OrdinalIgnoreCase)) is { Value: { } variant }
            ? variant
            : Action;

    public BoardButton Validated(IReadOnlySet<string> leagues)
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Button: Kennung");
        ValidationException.ThrowIfBlank(Label, nameof(Label), $"Button „{Id}“: Text");
        Action.Validated(Label);
        foreach (var league in Leagues ?? [])
            if (!leagues.Contains(league))
                throw new ValidationException(nameof(Leagues), $"Button „{Label}“: die Liga „{league}“ gibt es nicht.");
        foreach (var (league, variant) in PerLeague ?? new Dictionary<string, ButtonAction>())
        {
            if (!leagues.Contains(league))
                throw new ValidationException(nameof(PerLeague), $"Button „{Label}“: die Liga „{league}“ gibt es nicht.");
            variant.Validated(Label);
        }
        return this;
    }
}

public static class ButtonIds
{
    public static string Slug(string label)
    {
        var text = label.ToLowerInvariant().Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        var slug = new System.Text.StringBuilder();
        foreach (var c in text)
        {
            if (char.IsAsciiLetterOrDigit(c)) slug.Append(c);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }
        var result = slug.ToString().Trim('-');
        return result.Length > 0 ? result : "button";
    }

    public static string Next(string label, IEnumerable<string> taken)
    {
        var used = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var slug = Slug(label);
        var id = slug;
        for (var n = 2; used.Contains(id); n++) id = $"{slug}-{n}";
        return id;
    }
}

public sealed record Placement(int Number, string Button, bool OnDeck = true);

public sealed record NumberedRun(string Id, string Name, int Start, IReadOnlyList<string> Buttons)
{
    public IEnumerable<int> Span => Enumerable.Range(Start, Buttons.Count);
}

public sealed record BoardLayout(IReadOnlyList<Placement>? Keys = null, IReadOnlyList<NumberedRun>? Runs = null)
{
    public static readonly BoardLayout Empty = new();

    public IReadOnlyList<Placement> Placements => Keys ?? [];

    public IReadOnlyList<NumberedRun> Sequences => Runs ?? [];

    public IEnumerable<string> ButtonIds => Placements.Select(p => p.Button).Concat(Sequences.SelectMany(r => r.Buttons));
}

public sealed record TabLayout(string Tab, int Columns, int Rows, BoardLayout Layout)
{
    public bool Fits(BoardDeck deck) => deck.Columns == Columns && deck.Rows == Rows;
}

public sealed record BoardDeck(
    string Id,
    string Name,
    int Columns,
    int Rows,
    bool FollowsTab = true,
    BoardLayout? Own = null,
    BoardLayout? Fixed = null,
    int PanicKey = 0,
    int BackKey = 0)
{
    public int Count => Columns * Rows;

    public int Panic => PanicKey > 0 ? PanicKey : Count;

    public int Back => BackKey > 0 ? BackKey : Count - Columns + 1;

    public bool Reserved(int number) => number == Panic || number == Back;
}

public sealed record ResolvedKey(int Number, BoardButton Button, ButtonAction Action, int? Position, bool OnDeck);

public sealed record TabBoard(
    IReadOnlyList<Venue> Venues,
    IReadOnlyList<League> Leagues,
    IReadOnlyList<BoardButton> Buttons,
    IReadOnlyList<TabLayout> Layouts,
    IReadOnlyList<BoardDeck> Decks)
{
    public static readonly TabBoard Empty = new([], [], [], [], []);

    public bool IsEmpty => Venues.Count == 0 && Leagues.Count == 0 && Buttons.Count == 0;

    public BoardButton? Button(string? id) =>
        id is null ? null : Buttons.FirstOrDefault(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public BoardDeck? Deck(string? id) =>
        id is null ? null : Decks.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public BoardLayout LayoutFor(string tab, int columns, int rows) =>
        Layouts.FirstOrDefault(l => l.Tab == tab && l.Columns == columns && l.Rows == rows)?.Layout ?? BoardLayout.Empty;

    public BoardLayout LayoutOf(BoardDeck deck, string tab) =>
        deck.FollowsTab ? LayoutFor(tab, deck.Columns, deck.Rows) : deck.Own ?? BoardLayout.Empty;

    public IReadOnlyList<ResolvedKey> Resolve(BoardDeck deck, string tab, string league)
    {
        var keys = new Dictionary<int, ResolvedKey>();
        foreach (var layout in new[] { LayoutOf(deck, tab), deck.Fixed ?? BoardLayout.Empty })
        {
            foreach (var placement in layout.Placements)
            {
                if (keys.ContainsKey(placement.Number) || deck.Reserved(placement.Number)) continue;
                if (Button(placement.Button) is not { } button || !button.OfferedIn(league)) continue;
                keys[placement.Number] = new ResolvedKey(placement.Number, button, button.ActionIn(league), null, placement.OnDeck);
            }
            foreach (var run in layout.Sequences)
            {
                var offered = run.Buttons.Select(Button).OfType<BoardButton>().Where(b => b.OfferedIn(league)).ToList();
                for (var index = 0; index < offered.Count; index++)
                {
                    var number = run.Start + index;
                    if (keys.ContainsKey(number) || deck.Reserved(number) || number > deck.Count) continue;
                    keys[number] = new ResolvedKey(number, offered[index], offered[index].ActionIn(league), index + 1, true);
                }
            }
        }
        return keys.Values.OrderBy(k => k.Number).ToList();
    }

    public string NewButtonId(string label) => ButtonIds.Next(label, Buttons.Select(b => b.Id));

    public IReadOnlyList<string> UsesOf(string buttonId)
    {
        bool Uses(BoardLayout layout) => layout.ButtonIds.Contains(buttonId, StringComparer.OrdinalIgnoreCase);
        var uses = Layouts.Where(l => Uses(l.Layout)).Select(l => $"{BoardTabs.Label(l.Tab)} · {l.Columns}×{l.Rows}").ToList();
        foreach (var deck in Decks)
        {
            if (deck.Own is { } own && Uses(own)) uses.Add($"{deck.Name} · eigene Seite");
            if (deck.Fixed is { } fixedLayer && Uses(fixedLayer)) uses.Add($"{deck.Name} · auf jedem Tab");
        }
        return uses;
    }

    public TabBoard Validated()
    {
        Unique(Venues.Select(v => v.Validated().Id), "Halle");
        Unique(Leagues.Select(l => l.Validated().Id), "Liga");
        var leagues = Leagues.Select(l => l.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Unique(Buttons.Select(b => b.Validated(leagues).Id), "Button");
        Unique(Decks.Select(d => d.Id), "Deck");
        foreach (var deck in Decks)
        {
            ValidationException.ThrowIfBlank(deck.Name, nameof(BoardDeck.Name), "Deck: Name");
            if (deck.Columns is < 1 or > 16 || deck.Rows is < 1 or > 8)
                throw new ValidationException(nameof(BoardDeck.Columns), $"Deck „{deck.Name}“: 1 bis 16 Spalten, 1 bis 8 Zeilen.");
            if (deck.Panic < 1 || deck.Panic > deck.Count || deck.Back < 1 || deck.Back > deck.Count || deck.Panic == deck.Back)
                throw new ValidationException(nameof(BoardDeck.PanicKey), $"Deck „{deck.Name}“: PANIC und Zurück brauchen zwei verschiedene Plätze im Raster.");
            Check(deck.Own ?? BoardLayout.Empty, deck, $"{deck.Name}, eigene Seite");
            Check(deck.Fixed ?? BoardLayout.Empty, deck, $"{deck.Name}, auf jedem Tab");
        }
        foreach (var layout in Layouts)
        {
            if (!BoardTabs.Known(layout.Tab))
                throw new ValidationException(nameof(TabLayout.Tab), $"Den Tab „{layout.Tab}“ gibt es nicht.");
            foreach (var deck in Decks.Where(d => d.FollowsTab && layout.Fits(d)))
                Check(layout.Layout, deck, $"{BoardTabs.Label(layout.Tab)} auf {deck.Name}");
        }
        return this;
    }

    private void Check(BoardLayout layout, BoardDeck deck, string where)
    {
        var taken = new HashSet<int>();
        void Claim(int number, string what)
        {
            if (number < 1 || number > deck.Count)
                throw new ValidationException(nameof(Placement.Number), $"{where}: {what} liegt ausserhalb des Rasters.");
            if (deck.Reserved(number))
                throw new ValidationException(nameof(Placement.Number), $"{where}: {what} liegt auf PANIC oder Zurück.");
            if (!taken.Add(number))
                throw new ValidationException(nameof(Placement.Number), $"{where}: Platz {number} ist doppelt belegt.");
        }
        foreach (var placement in layout.Placements)
        {
            if (Button(placement.Button) is null)
                throw new ValidationException(nameof(Placement.Button), $"{where}: den Button „{placement.Button}“ gibt es nicht.");
            Claim(placement.Number, $"Platz {placement.Number}");
        }
        foreach (var run in layout.Sequences)
        {
            ValidationException.ThrowIfBlank(run.Name, nameof(NumberedRun.Name), $"{where}: Name der Reihe");
            if (run.Buttons.FirstOrDefault(id => Button(id) is null) is { } missing)
                throw new ValidationException(nameof(NumberedRun.Buttons), $"{where}, Reihe „{run.Name}“: den Button „{missing}“ gibt es nicht.");
            foreach (var number in run.Span) Claim(number, $"Reihe „{run.Name}“");
        }
    }

    private static void Unique(IEnumerable<string> ids, string what)
    {
        var twice = ids.GroupBy(id => id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
            throw new ValidationException(what, $"{what}: die Kennung „{twice.Key}“ gibt es zweimal.");
    }
}

public sealed record BoardSession(bool Active, string Venue, string League, string Tab)
{
    public static BoardSession Off(string league) => new(false, "", league, BoardTabs.Before);
}

public static class BoardCompiler
{
    public static Deck Compile(this TabBoard board, BoardDeck deck, string tab, string league, string? id = null) =>
        new(
            id ?? deck.Id,
            deck.Name,
            deck.Columns,
            deck.Rows,
            [new DeckPage(DeckPage.HomeId, BoardTabs.Label(tab), null, board.Resolve(deck, tab, league)
                .Where(key => key.OnDeck)
                .Select(ToKey)
                .ToList())],
            deck.Panic,
            deck.Back);

    public static string LeagueOf(this TabBoard board, string? gameLeague) =>
        board.Leagues.FirstOrDefault(l => l.Name.Equals(gameLeague ?? "", StringComparison.OrdinalIgnoreCase)
                || l.Id.Equals(gameLeague ?? "", StringComparison.OrdinalIgnoreCase))?.Id
            ?? board.Leagues.FirstOrDefault()?.Id
            ?? "";

    public static bool Drives(this TabBoard board, BoardDeck deck, string tab) =>
        board.LayoutOf(deck, tab).ButtonIds.Any() || (deck.Fixed?.ButtonIds.Any() ?? false);

    public static BoardDeck? DeckFitting(this TabBoard board, Deck deck) =>
        board.Decks.FirstOrDefault(d => d.Columns == deck.Columns && d.Rows == deck.Rows);

    private static DeckKey ToKey(ResolvedKey key) => new(
        key.Number,
        key.Action.Kind,
        key.Position is { } position ? $"{position}. {key.Button.Label}" : key.Button.Label,
        key.Button.Color,
        key.Button.Icon,
        key.Action.MusicId,
        key.Action.Special,
        Live: key.Action.Live,
        Steps: key.Action.Steps,
        Livestream: key.Action.Livestream,
        Led: key.Action.Led);
}
