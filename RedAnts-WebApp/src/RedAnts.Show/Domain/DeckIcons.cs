namespace RedAnts.Show.Domain;

public static class DeckIcons
{
    public const string Folder = "/_content/RedAnts.Show/show/icons/";

    public static IReadOnlyList<string> All { get; } =
    [
        "aufstellung",
        "aufwaermen",
        "ball",
        "bestplayer",
        "bully",
        "cafeteria",
        "coach",
        "drittel",
        "einblender-aus",
        "einblender-ein",
        "einlaufmusik",
        "geburtstag",
        "highlights",
        "kommentar",
        "kommentar-b",
        "meldungen",
        "nach-dem-spiel",
        "opener",
        "ordner",
        "penalty",
        "resultat",
        "schiedsrichter",
        "schiedsrichter-b",
        "spieler",
        "spielunterbruch",
        "starting-six",
        "starting6",
        "strafe",
        "strafe-gegner",
        "strafe-heim",
        "timeout",
        "tor-gegner",
        "tor-heim",
        "torbeschwoerung",
        "unihockey",
        "verlaengerung",
        "warten",
        "werbedurchsage-lidl",
        "werbedurchsage-mobiliar",
        "werbung",
    ];

    public static bool Known(string? icon) => icon is { Length: > 0 } value && All.Contains(value);

    public static string FileOf(string icon) => Folder + icon + ".png";

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
