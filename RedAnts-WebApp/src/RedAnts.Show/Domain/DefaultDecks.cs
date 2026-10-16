namespace RedAnts.Show.Domain;

public static class DefaultDecks
{
    public const string FieldDeck = "xl";
    public const string MusicDeck = "mk2";

    public const string MessagePage = "meldung";

    public static IReadOnlyList<Deck> All => [Field(), Music()];

    public static Deck Field() => new(FieldDeck, "Streamdeck XL (Halle)", 8, 4,
    [
        new DeckPage(DeckPage.HomeId, "Startseite", null,
        [
            Special(1, SpecialKey.GoalHome, "Tor", Home),
            Title(2, "Coach", "coach", Home, "coach", side: "home"),
            Title(3, "Timeout", "timeout", Home, "timeout", side: "home"),
            Title(4, "Aufstellung", "lineup_home", Home, "aufstellung", side: "home"),
            Title(5, "Aufstellung", "lineup_away", Away, "aufstellung", side: "away"),
            Title(6, "Timeout", "timeout", Away, "timeout", side: "away"),
            Title(7, "Coach", "coach", Away, "coach", side: "away"),
            Special(8, SpecialKey.GoalAway, "Tor", Away),

            Special(9, SpecialKey.PenaltyHome, "Strafe", Home),
            Special(10, SpecialKey.PlayerHome, "Spieler", Home),
            Special(11, SpecialKey.BestPlayerHome, "Best Player", Home),
            Special(12, SpecialKey.Starting6Home, "Starting 6", Home),
            Special(13, SpecialKey.Starting6Away, "Starting 6", Away),
            Special(14, SpecialKey.BestPlayerAway, "Best Player", Away),
            Special(15, SpecialKey.PlayerAway, "Spieler", Away),
            Special(16, SpecialKey.PenaltyAway, "Strafe", Away),

            new DeckKey(18, KeyKinds.Page, "Meldung", MessageColor, "meldungen", PageId: MessagePage),
            new DeckKey(19, KeyKinds.Live, "Drittel", Neutral, Live: LiveValues.Period),
            new DeckKey(20, KeyKinds.Live, "Stand Heim", Home, Live: LiveValues.HomeScore),
            new DeckKey(21, KeyKinds.Live, "Stand Gast", Away, Live: LiveValues.AwayScore),
            Title(22, "Highlight", "result", Highlight, "highlights"),
            Title(23, "Resultat", "result", Result, "resultat"),
            Title(24, "Opener", "opener", Opener, "opener"),

            Title(26, "Kommentar", "commentary", Info, "kommentar"),
            Title(27, "Schiri", "referee", Info, "schiedsrichter"),
            Led(28, "LED Pause", "led:pause"),
            Led(29, "LED Intermission", "led:intermission"),
            Special(30, SpecialKey.ClockToggle, "Uhr", StateOff),
            Led(31, "Werbung", "led:sponsoren", "werbung"),
            Special(32, SpecialKey.OverlayToggle, "Einblender", StateOff),
        ]),
        new DeckPage(MessagePage, "Meldung", DeckPage.HomeId,
        [
            Note(1, "Technik", "Technische Störung, wir sind gleich zurück"),
            Note(2, "Video", "Bildstörung, wir arbeiten daran"),
            Note(3, "Audio", "Tonstörung, wir arbeiten daran"),
            Note(9, "Highlights", "Die Highlights gibt es nach dem Spiel"),
            Note(10, "Ohne Kommentar", "Leider heute ohne Kommentar"),
            Note(11, "Live aus der Halle", "Live aus der Halle"),
            Special(8, SpecialKey.Spectators, "Zuschauer", Info),
        ]),
    ], PanicKey: 25, BackKey: 17);

    public static Deck Music() => new(MusicDeck, "Streamdeck Mk2 (DJ-Pult)", 5, 3,
    [
        new DeckPage(DeckPage.HomeId, "Startseite", null,
        [
            Special(11, SpecialKey.MusicNext, "Nächstes Stück", Music1),
            Special(12, SpecialKey.MusicStop, "Stopp", Stop),
            Special(13, SpecialKey.MusicFade, "Fade-out", Fade),
            Special(14, SpecialKey.AutoBreakMusic, "Automodus", StateOff),
        ]),
    ], PanicKey: 15, BackKey: 10);

    private const string Home = "#e23b3b";
    private const string Away = "#3b82e2";
    private const string MessageColor = "#8e44ad";
    private const string Info = "#4a5568";
    private const string Neutral = "#2b3038";
    private const string Highlight = "#00bcd4";
    private const string Result = "#e67e22";
    private const string Opener = "#c026d3";
    private const string StateOff = "#1e7b34";
    private const string Music1 = "#2f855a";
    private const string Stop = "#c0201c";
    private const string Fade = "#ff6600";

    private static DeckKey Special(int number, string special, string label, string color) =>
        new(number, KeyKinds.Special, label, color, SpecialKey.Of(special)?.Icon is { Length: > 0 } icon ? icon : null, Special: special);

    private static DeckKey Title(int number, string label, string kind, string color, string? icon = null, bool home = false, string side = "") =>
        new(number, KeyKinds.Steps, label, color, icon, Steps:
        [
            new KeyStep(KeyActions.VmixShow, "title:" + kind, Event: kind, Side: side),
            new KeyStep(KeyActions.Wait, Ms: 8000),
            new KeyStep(KeyActions.VmixHide, "title:" + kind),
        ], HomeAfter: home);

    private static DeckKey Note(int number, string label, string text) =>
        new(number, KeyKinds.Steps, label, "#4a5568", "meldungen", Steps:
        [
            new KeyStep(KeyActions.VmixShow, "title:message", Text: text),
            new KeyStep(KeyActions.Wait, Ms: 8000),
            new KeyStep(KeyActions.VmixHide, "title:message"),
        ], HomeAfter: true);

    private static DeckKey Led(int number, string label, string playlist, string? icon = null) =>
        new(number, KeyKinds.Steps, label, StateOff, icon, Steps: [new KeyStep(KeyActions.LedStart, playlist)]);
}

public static class LiveValues
{
    public const string Time = "zeit";
    public const string Period = "drittel";
    public const string HomeScore = "stand.heim";
    public const string AwayScore = "stand.gast";

    public static readonly IReadOnlyList<string> All = [Time, Period, HomeScore, AwayScore];

    public static string Label(string value) => value switch
    {
        Time => "Spielzeit",
        Period => "Drittel",
        HomeScore => "Stand Heim",
        AwayScore => "Stand Gast",
        _ => value,
    };
}
