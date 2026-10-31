namespace RedAnts.Show.Domain;

public static class KeyKinds
{
    public const string Empty = "leer";
    public const string Music = "musik";
    public const string Steps = "befehle";
    public const string Special = "spezial";
    public const string Page = "seite";
    public const string Live = "anzeige";

    public static readonly IReadOnlyList<string> All = [Empty, Music, Steps, Special, Page, Live];

    public static string Label(string kind) => kind switch
    {
        Empty => "leer",
        Music => "Musik",
        Steps => "Befehlsfolge",
        Special => "Feste Funktion",
        Page => "Seite öffnen",
        Live => "Anzeige",
        _ => kind,
    };
}

public static class KeyActions
{
    public const string MusicPlay = "musik.start";
    public const string MusicStop = "musik.stopp";
    public const string MusicFade = "musik.fade";
    public const string VmixShow = "vmix.ein";
    public const string VmixText = "vmix.text";
    public const string VmixHide = "vmix.aus";
    public const string LedStart = "led.start";
    public const string LedStop = "led.stopp";
    public const string Wait = "warten";

    public static readonly IReadOnlyList<string> All =
        [MusicPlay, MusicStop, MusicFade, VmixShow, VmixText, VmixHide, LedStart, LedStop, Wait];

    public static string Label(string action) => action switch
    {
        MusicPlay => "Musik abspielen",
        MusicStop => "Musik stoppen",
        MusicFade => "Musik ausblenden",
        VmixShow => "Einblender zeigen",
        VmixText => "Text anzeigen",
        VmixHide => "Einblender ausblenden",
        LedStart => "LED-Playlist starten",
        LedStop => "LED-Playlist stoppen",
        Wait => "Warten",
        _ => action,
    };
}

public sealed record SpecialKey(string Id, string Label, string Group, string Icon = "")
{
    public const string GoalHome = "tor-heim";
    public const string GoalAway = "tor-gast";
    public const string PenaltyHome = "strafe-heim";
    public const string PenaltyAway = "strafe-gast";
    public const string BestPlayerHome = "bestplayer-heim";
    public const string BestPlayerAway = "bestplayer-gast";
    public const string PlayerHome = "spieler-heim";
    public const string PlayerAway = "spieler-gast";
    public const string Starting6Home = "starting6-heim";
    public const string Starting6Away = "starting6-gast";
    public const string OverlayToggle = "einblender";
    public const string ClockToggle = "uhr";
    public const string MusicToggle = "musik-an-aus";
    public const string AutoBreakMusic = "auto-pausenmusik";
    public const string MusicStop = "musik-stopp";
    public const string MusicFade = "musik-fade";
    public const string MusicNext = "musik-vor";
    public const string MusicPrevious = "musik-zurueck";
    public const string MusicPause = "musik-pause";
    public const string Spectators = "zuschauer";

    public static IReadOnlyList<SpecialKey> All { get; } =
    [
        new(GoalHome, "Tor Heim", "Spiel", "ball"),
        new(GoalAway, "Tor Gast", "Spiel", "ball"),
        new(PenaltyHome, "Strafe Heim", "Spiel", "strafe"),
        new(PenaltyAway, "Strafe Gast", "Spiel", "strafe"),
        new(BestPlayerHome, "Best Player Heim", "Spiel", "bestplayer"),
        new(BestPlayerAway, "Best Player Gast", "Spiel", "bestplayer"),
        new(PlayerHome, "Spielerin Heim", "Spiel", "spieler"),
        new(PlayerAway, "Spielerin Gast", "Spiel", "spieler"),
        new(Starting6Home, "Starting 6 Heim", "Spiel", "starting6"),
        new(Starting6Away, "Starting 6 Gast", "Spiel", "starting6"),
        new(OverlayToggle, "Einblender an/aus", "Zustand", "einblender-ein"),
        new(ClockToggle, "Uhr an/aus", "Zustand", "⏱"),
        new(MusicToggle, "Musik an/aus", "Zustand", "🔊"),
        new(AutoBreakMusic, "Automodus Pausenmusik", "Zustand", "🔁"),
        new(MusicStop, "Musik Stopp", "Musik", "⏹"),
        new(MusicFade, "Musik Fade-out", "Musik", "🔉"),
        new(MusicNext, "Nächstes Stück", "Musik", "⏭"),
        new(MusicPrevious, "Vorheriges Stück", "Musik", "⏮"),
        new(MusicPause, "Pause und Weiter", "Musik", "⏸"),
        new(Spectators, "Zuschauerzähler", "Spiel"),
    ];

    public static SpecialKey? Of(string? id) => All.FirstOrDefault(s => s.Id == id);

    public static bool ChoosesPlayer(string? id) => id is GoalHome or GoalAway or PenaltyHome or PenaltyAway
        or BestPlayerHome or BestPlayerAway or PlayerHome or PlayerAway or Starting6Home or Starting6Away;

    public static bool NeedsVmix(string id) => id is GoalHome or GoalAway or PenaltyHome or PenaltyAway
        or BestPlayerHome or BestPlayerAway or PlayerHome or PlayerAway or Starting6Home or Starting6Away
        or OverlayToggle or ClockToggle or Spectators;

    public static bool NeedsSound(string id) => id is MusicToggle or AutoBreakMusic or MusicStop
        or MusicFade or MusicNext or MusicPrevious or MusicPause;

    public static bool NeedsClock(string id) => id is AutoBreakMusic or ClockToggle;

    public static bool Offered(string id, bool sound, bool clock, bool vmix) =>
        (!NeedsSound(id) || sound) && (!NeedsClock(id) || clock) && (!NeedsVmix(id) || vmix);
}

public sealed record KeyStep(string Action, string? Target = null, int? Ms = null, string? Event = null, string Side = "", string Text = "")
{
    public KeyStep Validated()
    {
        if (!KeyActions.All.Contains(Action))
            throw new ValidationException(nameof(Action), $"Unbekannter Befehl „{Action}“.");
        if (Ms is < 0 or > 120000)
            throw new ValidationException(nameof(Ms), "Wartezeit: 0 bis 120000 Millisekunden.");
        return this;
    }
}

public sealed record DeckKey(
    int Number,
    string Kind,
    string Label = "",
    string Color = DeckKey.DefaultColor,
    string? Icon = null,
    string? MusicId = null,
    string? Special = null,
    string? PageId = null,
    string? Live = null,
    IReadOnlyList<KeyStep>? Steps = null,
    bool HomeAfter = false,
    bool Livestream = true,
    bool Led = true)
{
    public const string DefaultColor = "#2b3038";

    public IReadOnlyList<KeyStep> Sequence => Steps ?? [];

    public bool IsEmpty => Kind == KeyKinds.Empty;

    public DeckKey Validated(int count)
    {
        if (Number < 1 || Number > count)
            throw new ValidationException(nameof(Number), $"Taste {Number} liegt ausserhalb des Rasters.");
        if (!KeyKinds.All.Contains(Kind))
            throw new ValidationException(nameof(Kind), $"Unbekannte Tastenart „{Kind}“.");
        if (Kind == KeyKinds.Special && SpecialKey.Of(Special) is null)
            throw new ValidationException(nameof(Special), "Feste Funktion: bitte eine auswählen.");
        if (Kind == KeyKinds.Page && string.IsNullOrWhiteSpace(PageId))
            throw new ValidationException(nameof(PageId), "Seite öffnen: bitte eine Seite auswählen.");
        if (Kind == KeyKinds.Music && string.IsNullOrWhiteSpace(MusicId))
            throw new ValidationException(nameof(MusicId), "Musik: bitte einen Befehl auswählen.");
        foreach (var step in Sequence) step.Validated();
        return this;
    }
}

public sealed record DeckPage(string Id, string Name, string? ParentId, IReadOnlyList<DeckKey> Keys)
{
    public const string HomeId = "start";

    public bool IsHome => Id == HomeId;

    public DeckKey KeyAt(int number) =>
        Keys.FirstOrDefault(k => k.Number == number) ?? new DeckKey(number, KeyKinds.Empty);

    public DeckPage With(DeckKey key) =>
        this with { Keys = Keys.Where(k => k.Number != key.Number).Append(key).OrderBy(k => k.Number).ToList() };

    public DeckPage Validated(int count)
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Name");
        foreach (var key in Keys) key.Validated(count);
        return this;
    }
}

public sealed record PageSet(int Columns, int Rows, IReadOnlyList<DeckPage> Pages)
{
    public int Count => Columns * Rows;

    public bool Fits(Deck deck) => deck.Columns == Columns && deck.Rows == Rows;

    public DeckPage? Page(string? id) => id is null ? null : Pages.FirstOrDefault(p => p.Id == id);

    public PageSet With(DeckPage page) =>
        Page(page.Id) is null
            ? this with { Pages = [.. Pages, page] }
            : this with { Pages = Pages.Select(p => p.Id == page.Id ? page : p).ToList() };

    public PageSet Without(string id) => this with { Pages = Pages.Where(p => p.Id != id).ToList() };

    public PageSet Validated()
    {
        if (Columns is < 1 or > 16 || Rows is < 1 or > 8)
            throw new ValidationException(nameof(Columns), "Seitensatz: 1 bis 16 Spalten, 1 bis 8 Zeilen.");
        foreach (var page in Pages)
        {
            if (page.IsHome)
                throw new ValidationException(nameof(Pages), "Die Startseite gehört zum Deck, nicht zu den geteilten Seiten.");
            page.Validated(Count);
        }
        var twice = Pages.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
            throw new ValidationException(nameof(Pages), $"Die Seitenkennung „{twice.Key}“ gibt es zweimal.");
        return this;
    }
}

public sealed record Deck(
    string Id,
    string Name,
    int Columns,
    int Rows,
    IReadOnlyList<DeckPage> Pages,
    int PanicKey = 0,
    int BackKey = 0)
{
    public int Count => Columns * Rows;

    public int Panic => PanicKey > 0 ? PanicKey : Count;

    public int Back => BackKey > 0 ? BackKey : Count - Columns + 1;

    public DeckPage Home => Pages.FirstOrDefault(p => p.IsHome) ?? Pages.FirstOrDefault() ?? Empty(DeckPage.HomeId, "Startseite");

    public DeckPage? Page(string? id) => id is null ? null : Pages.FirstOrDefault(p => p.Id == id);

    public bool Reserved(int number, bool home = false) => number == Panic || (!home && number == Back);

    public static DeckPage Empty(string id, string name, string? parent = null) => new(id, name, parent, []);

    public Deck Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Name");
        if (Columns is < 1 or > 16 || Rows is < 1 or > 8)
            throw new ValidationException(nameof(Columns), "Deck-Raster: 1 bis 16 Spalten, 1 bis 8 Zeilen.");
        if (Panic < 1 || Panic > Count)
            throw new ValidationException(nameof(PanicKey), "PANIC liegt ausserhalb des Rasters.");
        if (Back < 1 || Back > Count)
            throw new ValidationException(nameof(BackKey), "Zurück liegt ausserhalb des Rasters.");
        if (Panic == Back)
            throw new ValidationException(nameof(BackKey), "PANIC und Zurück brauchen verschiedene Plätze.");
        if (Pages.Count == 0)
            throw new ValidationException(nameof(Pages), "Ein Deck braucht mindestens die Startseite.");
        if (Pages.All(p => !p.IsHome))
            throw new ValidationException(nameof(Pages), $"Ein Deck braucht die Startseite mit der Kennung „{DeckPage.HomeId}“.");
        foreach (var page in Pages) page.Validated(Count);
        var twice = Pages.GroupBy(p => p.Id).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
            throw new ValidationException(nameof(Pages), $"Die Seitenkennung „{twice.Key}“ gibt es zweimal.");
        return this;
    }
}
