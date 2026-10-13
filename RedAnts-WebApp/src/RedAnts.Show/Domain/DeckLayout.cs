namespace RedAnts.Show.Domain;

public static class DeckKeyKinds
{
    public const string Empty = "leer";
    public const string Scene = "szene";
    public const string Sound = "musik";
    public const string Page = "seite";
    public const string Live = "anzeige";
    public const string State = "zustand";
    public const string Back = "zurueck";

    public static readonly IReadOnlyList<string> All = [Empty, Scene, Sound, Page, Live, State, Back];
}

public sealed record DeckStateRow(string Value, string Label, string? Icon, string Color, string Behaviour, string? SceneId)
{
    public const string RunScene = "szene";
    public const string Locked = "gesperrt";
    public const string Nothing = "nichts";
}

public sealed record DeckKey(
    int Number,
    string Kind,
    string Label = "",
    string Color = "#2b3038",
    string? Icon = null,
    string? SceneId = null,
    string? SoundId = null,
    string? Page = null,
    string? Live = null,
    string? StateId = null,
    IReadOnlyList<DeckStateRow>? Rows = null);

public sealed record DeckLayout(string Id, string Name, int Columns, int Rows, IReadOnlyList<DeckKey> Keys)
{
    public const string FieldDeck = "xl";
    public const string MusicDeck = "mk2";

    public int Count => Columns * Rows;

    public DeckKey KeyAt(int number) =>
        Keys.FirstOrDefault(k => k.Number == number) ?? new DeckKey(number, DeckKeyKinds.Empty);

    public DeckLayout With(DeckKey key) =>
        this with { Keys = Keys.Where(k => k.Number != key.Number).Append(key).OrderBy(k => k.Number).ToList() };

    public DeckLayout Validated()
    {
        ValidationException.ThrowIfBlank(Id, nameof(Id), "Kennung");
        ValidationException.ThrowIfBlank(Name, nameof(Name), "Name");
        if (Columns is < 1 or > 16 || Rows is < 1 or > 8) throw new ValidationException(nameof(Columns), "Deck-Raster: 1 bis 16 Spalten, 1 bis 8 Zeilen.");
        var outside = Keys.FirstOrDefault(k => k.Number < 1 || k.Number > Count);
        if (outside is not null) throw new ValidationException(nameof(Keys), $"Taste {outside.Number} liegt ausserhalb des Rasters.");
        return this;
    }
}

public static class DefaultDecks
{
    public const int BackKey = 17;
    public const int PanicKey = 25;

    public static IReadOnlyList<DeckLayout> All { get; } = [Field(), Music()];

    public static DeckLayout Field() => new(DeckLayout.FieldDeck, "Streamdeck XL (Halle)", 8, 4,
    [
        Scene(1, "Tor", "#ff7c80", "tor-heim", "ball"), Scene(2, "Coach", "#ff7c80", "coach-heim", "coach"),
        Scene(3, "Timeout", "#ff7c80", "timeout-heim", "timeout"), Scene(4, "Aufstellung", "#ff7c80", "aufstellung-heim", "aufstellung"),
        Scene(5, "Aufstellung", "#a6c9ec", "aufstellung-gast", "aufstellung"), Scene(6, "Timeout", "#a6c9ec", "timeout-gast", "timeout"),
        Scene(7, "Coach", "#a6c9ec", "coach-gast", "coach"), Scene(8, "Tor", "#a6c9ec", "tor-gast", "ball"),
        Page(9, "Strafe", "#ff7c80", "PenaltyHome", "strafe"), Scene(10, "Spieler", "#ff7c80", "name-heim", "spieler"),
        Scene(11, "Best Player", "#ff7c80", "bestplayer-heim", "bestplayer"), Scene(12, "Starting 6", "#ff7c80", "starting6-heim", "starting6"),
        Scene(13, "Starting 6", "#a6c9ec", "starting6-gast", "starting6"), Scene(14, "Best Player", "#a6c9ec", "bestplayer-gast", "bestplayer"),
        Scene(15, "Spieler", "#a6c9ec", "name-gast", "spieler"), Page(16, "Strafe", "#a6c9ec", "PenaltyAway", "strafe"),
        new(BackKey, DeckKeyKinds.Back, "◀ Zurück", "#8ed973"),
        Page(18, "Mitteilung", "#c1f0c8", "Message", "meldungen"),
        Live(19, "Drittel", "#d86dcd", "period"), Live(20, "Stand Heim", "#ff7c80", "score.home"), Live(21, "Stand Gast", "#a6c9ec", "score.away"),
        Scene(22, "Highlight", "#00e5ff", "highlight", "highlights"), Scene(23, "Resultat", "#f1a983", "resultat", "resultat"),
        Scene(24, "Opener", "#d86dcd", "opener", "opener"),
        Scene(PanicKey, "🚨 PANIC", "#ff0000", "panic"),
        Scene(26, "Kommentar", "#d9d9d9", "kommentatoren", "kommentar"), Scene(27, "Schiri", "#d9d9d9", "schiedsrichter", "schiedsrichter"),
        Toggle(28, DefaultStates.Led, "LED Pause", "pause", "sys-led-stopp"),
        Toggle(29, DefaultStates.Led, "LED Intermission", "intermission", "sys-led-stopp"),
        Toggle(30, DefaultStates.ClockVisible, "Uhr", "sys-uhr-ein", "sys-uhr-aus"),
        Toggle(31, DefaultStates.Sponsors, "Werbung", "sponsoren", "sys-led-stopp", "werbung"),
        Toggle(32, DefaultStates.Overlay, "Einblender", "sys-einblenden", "sys-ausblenden", "einblender-ein"),
    ]);

    public static DeckLayout Music() => new(DeckLayout.MusicDeck, "Streamdeck Mk2 (DJ-Pult)", 5, 3,
    [
        new(11, DeckKeyKinds.Scene, "Stopp", "#ff0000", SceneId: "sys-musik-stopp"),
        new(13, DeckKeyKinds.State, "Auto-Pausenmusik", "#1e7b34", StateId: DefaultStates.AutoBreakMusic, Rows:
        [
            new("aus", "Auto aus", null, "#3a3f47", DeckStateRow.RunScene, "sys-automusik-ein"),
            new("an", "Auto an", null, "#1e7b34", DeckStateRow.RunScene, "sys-automusik-aus"),
        ]),
        new(12, DeckKeyKinds.State, "Fade out", "#ff6600", StateId: DefaultStates.Music, Rows:
        [
            new("aus", "Fade out", null, "#3a3f47", DeckStateRow.Locked, null),
            new("läuft", "Fade out", null, "#ff6600", DeckStateRow.RunScene, "sys-musik-fade"),
            new("fade", "…fade", null, "#3a3f47", DeckStateRow.Locked, null),
        ]),
    ]);

    private static DeckKey Scene(int number, string label, string color, string scene, string? icon = null) =>
        new(number, DeckKeyKinds.Scene, label, color, icon, SceneId: scene);

    private static DeckKey Page(int number, string label, string color, string page, string? icon = null) =>
        new(number, DeckKeyKinds.Page, label, color, icon, Page: page);

    private static DeckKey Live(int number, string label, string color, string live) =>
        new(number, DeckKeyKinds.Live, label, color, Live: live);

    private static DeckKey Toggle(int number, string state, string label, string sceneOff, string sceneOn, string? icon = null) =>
        new(number, DeckKeyKinds.State, label, "#1e7b34", icon, StateId: state, Rows:
        [
            new(DefaultStates.All.First(s => s.Id == state).Values[0], label, icon, "#1e7b34", DeckStateRow.RunScene, sceneOff),
            new(DefaultStates.All.First(s => s.Id == state).Values[1], label, icon, "#c0201c", DeckStateRow.RunScene, sceneOn),
        ]);
}
