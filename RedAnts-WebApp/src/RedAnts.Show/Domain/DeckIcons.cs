namespace RedAnts.Show.Domain;

public static class DeckIcons
{
    public const string Folder = "/_content/RedAnts.Show/show/icons/";

    public static IReadOnlyList<string> All { get; } =
    [
        "audio",
        "aufstellung",
        "aufwaermen",
        "aufwaermen-hand",
        "aufwaermen-neu",
        "aufwaermen-schuhe",
        "aufwaermen-tafel",
        "aufwaermen-uhr",
        "ball",
        "bestplayer",
        "bully",
        "cafeteria",
        "coach",
        "drittel",
        "einblender-aus",
        "einblender-ein",
        "einlaufmusik",
        "einlaufmusik-neu",
        "fade-out",
        "fanshop",
        "geburtstag",
        "highlights",
        "highlights-film",
        "kommentar",
        "kommentar-b",
        "kommentatoren",
        "liga-1",
        "liga-lupl",
        "liga-u17",
        "liga-u21",
        "live-aus-der-halle",
        "meldungen",
        "musik-pausenspiel",
        "musik-waehrend-pause-neu",
        "mute",
        "nach-dem-spiel",
        "nach-dem-spiel-neu",
        "nach-dem-spiel-pokal",
        "ohne-kommentar",
        "opener",
        "ordner",
        "panic",
        "pause",
        "pausenspiel",
        "pausenspiel-werbung",
        "penalty",
        "penalty-neu",
        "play",
        "resultat",
        "schiedsrichter",
        "schiedsrichter-b",
        "spieldauerstrafe-karte",
        "spieldauerstrafe-uhr",
        "spieler",
        "spielunterbruch",
        "sponsor-bronze",
        "sponsor-gold",
        "sponsor-kooperation",
        "sponsor-medical",
        "sponsor-medien",
        "sponsor-nachwuchs",
        "sponsor-silber",
        "starting-six",
        "starting6",
        "stoerung-technik",
        "stoerung-ton",
        "stopp",
        "strafe",
        "strafe-10min",
        "strafe-2-2min",
        "strafe-2min",
        "strafe-gegner",
        "strafe-heim",
        "team-bern",
        "team-chur",
        "team-jets",
        "team-laupen",
        "team-redants",
        "team-riders",
        "team-skorps",
        "team-uri",
        "team-wizards",
        "team-zug",
        "timeout",
        "tor-gegner",
        "tor-heim",
        "torbeschwoerung",
        "unihockey",
        "verlaengerung",
        "vor",
        "vor-dem-spiel",
        "waehrend-pause",
        "warten",
        "werbedurchsage-lidl",
        "werbedurchsage-mobiliar",
        "werbung",
        "werbung-gastro",
        "zurueck",
        "zuschauerzahlen",
    ];

    public static bool Known(string? icon) => icon is { Length: > 0 } value && All.Contains(value);

    private static readonly string Build = Stamp();

    private static string Stamp()
    {
        try
        {
            var file = typeof(DeckIcons).Assembly.Location;
            if (file.Length > 0 && System.IO.File.Exists(file))
                return System.IO.File.GetLastWriteTimeUtc(file).Ticks.ToString();
        }
        catch (System.IO.IOException) { }
        catch (UnauthorizedAccessException) { }
        return Environment.TickCount64.ToString();
    }

    public static string FileOf(string icon) => $"{Folder}{icon}.png?v={Build}";

    public static IReadOnlyList<DeckSymbol> Symbols { get; } =
    [
        new("▶", "Musik", "Start"),
        new("⏸", "Musik", "Pause"),
        new("⏹", "Musik", "Stopp"),
        new("⏭", "Musik", "Vor"),
        new("⏮", "Musik", "Zurück"),
        new("⏯", "Musik", "Start und Pause"),
        new("🔁", "Musik", "Wiederholen"),
        new("🔀", "Musik", "Zufall"),
        new("🎵", "Musik", "Musik"),
        new("🎶", "Musik", "Playlist"),
        new("🔊", "Ton", "Laut"),
        new("🔉", "Ton", "Leiser"),
        new("🔈", "Ton", "Leise"),
        new("🔇", "Ton", "Stumm"),
        new("🎙", "Ton", "Mikrofon"),
        new("📢", "Ton", "Durchsage"),
        new("⏱", "Spiel", "Uhr"),
        new("⏲", "Spiel", "Timer"),
        new("🚨", "Spiel", "Alarm"),
        new("🏆", "Spiel", "Pokal"),
        new("⭐", "Spiel", "Stern"),
        new("🔥", "Spiel", "Feuer"),
        new("📺", "Technik", "Bildschirm"),
        new("🎬", "Technik", "Szene"),
        new("💡", "Technik", "Licht"),
        new("✨", "Technik", "Effekt"),
        new("⚠", "Technik", "Warnung"),
        new("✅", "Technik", "Bereit"),
        new("❌", "Technik", "Aus"),
        new("ℹ", "Technik", "Info"),
    ];
}

public sealed record DeckSymbol(string Glyph, string Group, string Label);
