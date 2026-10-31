namespace RedAnts.Show.Domain;

public static class DefaultDecks
{
    public const string FieldDeck = "xl";
    public const string MusicDeck = "mk2";

    public const string MessagePage = "meldung";
    public const string TeamLogoPage = "teamlogos";

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

            TeamLogoKey(17),
            new DeckKey(18, KeyKinds.Page, "Meldung", MessageColor, "meldungen", PageId: MessagePage),
            RedAntsLogoKey(19),
            ScoreKey(20),
            GuestLogoKey(21),
            Title(22, "Highlight", "highlight", Highlight, "highlights"),
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
        TeamLogos(),
    ], PanicKey: 25, BackKey: 17);

    public static DeckKey TeamLogoKey(int number) =>
        new(number, KeyKinds.Page, "Teamlogos", Info, "unihockey", PageId: TeamLogoPage);

    public static DeckPage TeamLogos() => new(TeamLogoPage, "Teamlogos", DeckPage.HomeId,
    [
        Led(1, "Red Ants", "led:logo", "team-redants", Home),
        Led(2, "Berner Oberland", "led:gast-bern", "team-bern", Away),
        Led(3, "Chur United", "led:gast-chur", "team-chur", Away),
        Led(4, "Jets", "led:gast-jets", "team-jets", Away),
        Led(5, "Laupen", "led:gast-laupen", "team-laupen", Away),
        Led(6, "Riders", "led:gast-riders", "team-riders", Away),
        Led(7, "Skorpion", "led:gast-skorps", "team-skorps", Away),
        Led(8, "Uri", "led:gast-uri", "team-uri", Away),
        Led(9, "Wizards", "led:gast-wizards", "team-wizards", Away),
        Led(10, "Zug United", "led:gast-zug", "team-zug", Away),
        GuestLogoKey(11),
    ]);

    public static DeckKey RedAntsLogoKey(int number) => Led(number, "Red Ants", "led:logo", "team-redants", Home);

    public static DeckKey ScoreKey(int number) =>
        new(number, KeyKinds.Live, "Stand", Neutral, Live: LiveValues.Score);

    public static DeckKey GuestLogoKey(int number) =>
        new(number, KeyKinds.Special, "Logo Gast", Away, SpecialKey.Of(SpecialKey.GuestLogo)?.Icon, Special: SpecialKey.GuestLogo);

    public static Deck Music() => new(MusicDeck, "Streamdeck Mk2 (DJ-Pult)", 5, 3,
    [
        new DeckPage(DeckPage.HomeId, "Startseite", null,
        [
            Special(10, SpecialKey.AutoBreakMusic, "Automodus", StateOff),
            .. Transport(),
        ]),
    ], PanicKey: 11, BackKey: 1);

    public static IReadOnlyList<DeckKey> Transport() =>
    [
        Special(12, SpecialKey.MusicStop, "Stopp", Stop),
        Special(13, SpecialKey.MusicPause, "Pause", Music1),
        Special(14, SpecialKey.MusicNext, "Vor", Music1),
        Special(15, SpecialKey.MusicFade, "Fade-out", Fade),
    ];

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
        ], HomeAfter: home);

    private static DeckKey Note(int number, string label, string text) =>
        new(number, KeyKinds.Steps, label, "#4a5568", "meldungen", Steps:
        [
            new KeyStep(KeyActions.VmixShow, "title:message", Text: text),
            new KeyStep(KeyActions.Wait, Ms: 8000),
            new KeyStep(KeyActions.VmixHide, "title:message"),
        ], HomeAfter: true);

    private static DeckKey Led(int number, string label, string playlist, string? icon = null, string color = StateOff) =>
        new(number, KeyKinds.Steps, label, color, icon, Steps: [new KeyStep(KeyActions.LedStart, playlist)]);
}

public static class LiveValues
{
    public const string Time = "zeit";
    public const string Period = "drittel";
    public const string Score = "stand";
    public const string HomeScore = "stand.heim";
    public const string AwayScore = "stand.gast";
    public const string Overlay = "einblender";

    public static readonly IReadOnlyList<string> All = [Time, Period, Score, HomeScore, AwayScore, Overlay];

    public static string Label(string value) => value switch
    {
        Time => "Spielzeit",
        Period => "Drittel",
        Score => "Stand",
        HomeScore => "Stand Heim",
        AwayScore => "Stand Gast",
        Overlay => "Auf Sendung",
        _ => value,
    };
}
