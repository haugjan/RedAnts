namespace RedAnts.Features.Show;

// Feste Prototyp-Konfiguration (portiert aus dem SPA-Prototyp). Wird in einer
// späteren Phase durch einen Editor und Persistenz in der Datenbank ersetzt.
// Sound-Refs sind relativ zur Blob-Basis-URL (Show:Storage:PublicBaseUrl).
public static class ShowConfig
{
    private static ShowButton Local(string id, string label, string icon, string color, string file, string? subtitle = null,
        TileSize size = TileSize.Normal, int? dur = null)
        => new(id, label, icon, color, size, null, new ShowSound(SoundKind.Local, $"sounds/{file}", 0, dur), subtitle);

    private static ShowButton Spot(string id, string label, string icon, string color, string uri,
        int start, int dur, string? subtitle = null, TileSize size = TileSize.Normal)
        => new(id, label, icon, color, size, null, new ShowSound(SoundKind.Spotify, uri, start, dur), subtitle);

    private static ShowButton Folder(string id, string label, string icon, string color, params ShowButton[] children)
        => new(id, label, icon, color, TileSize.Normal, children, null, null);

    private static ShowButton Random(string id, string label, string icon, string color, string subtitle,
        TileSize size, params string[] files)
        => new(id, label, icon, color, size, null, null, subtitle,
            files.Select(f => new ShowSound(SoundKind.Local, $"sounds/{f}")).ToList());

    private static readonly string[] FunPool =
    [
        "01_Awkward-Silence_Crickets.mp3", "02_Schlechter-Witz_Ba-dum-tss.mp3", "06_Dramatik_Dun-dun-dun.mp3",
        "08_Peinlicher-Fehler_Womp-Womp.mp3", "09_Skepsis_Sad-Trombone.mp3", "14_Record-Scratch.mp3",
        "19_Vine-Boom.mp3", "21_Slide-Whistle.mp3",
    ];

    public static IReadOnlyList<ShowProfile> Profiles { get; } =
    [
        new ShowProfile("lupl", "L-UPL", "#C8102E",
        [
            Local("lupl-goal", "Tor!", "🥅", "#C8102E", "17_Boxing-Gong.mp3", "Boxing Gong", TileSize.Big),
            Local("lupl-applause", "Applaus", "👏", "#2a9d8f", "15_Applaus.mp3", "Applaus", TileSize.Wide),
            Local("lupl-buzzer", "Buzzer", "🚨", "#f4a261", "11_Stopp-Fokus_Buzzer.mp3", "Buzzer"),
            Local("lupl-oooh", "Ooooh!", "📣", "#457b9d", "18_Crowd-Ooooh.mp3", "Crowd"),
            Local("lupl-timeout", "Timeout", "⏱️", "#6d597a", "07_Zeitdruck_Clock-Ticking.mp3", "Clock Ticking", TileSize.Tall, 20),
            Spot("lupl-fansong", "Seven Nation Army", "🎶", "#1db954", "spotify:track:3dPQuX8Gs42Y7b454ybpMR", 0, 45, "The White Stripes", TileSize.Wide),
            Random("lupl-random", "Überraschung", "🎲", "#e07a1f", "Zufälliger Fun-Sound", TileSize.Normal, FunPool),
            Folder("lupl-einlauf", "Einlauf", "🎵", "#1db954",
                Spot("lupl-einlauf-thunderstruck", "Thunderstruck", "⚡", "#1db954", "spotify:track:57bgtoPSgt236HzfBOd8kj", 31, 90, "AC/DC · ab 0:31", TileSize.Wide),
                Spot("lupl-einlauf-sandstorm", "Sandstorm", "🌪️", "#1db954", "spotify:track:6Sy9BUbgFse0n0LPA5lwy5", 44, 45, "Darude · ab 0:44"),
                Spot("lupl-einlauf-eye", "Eye of the Tiger", "🐯", "#1db954", "spotify:track:2KH16WveTQWT6KOG9Rg6e2", 0, 60, "Survivor"),
                Local("lupl-einlauf-drumroll", "Trommelwirbel", "🥁", "#b5838d", "13_Trommelwirbel.mp3", "Drumroll")),
            Folder("lupl-jingles", "Jingles", "🎬", "#457b9d",
                Local("lupl-jingle-boom", "Boom", "💥", "#e76f51", "19_Vine-Boom.mp3", "Vine Boom"),
                Local("lupl-jingle-coin", "Mario Coin", "🪙", "#e9c46a", "22_Mario-Coin.mp3", "Coin"),
                Local("lupl-jingle-kaching", "Ka-ching", "💰", "#2a9d8f", "16_Ka-ching.mp3", "Ka-ching"),
                Local("lupl-jingle-chor", "Himml. Chor", "😇", "#8ecae6", "20_Himmlischer-Chor.mp3", "Chor"),
                Local("lupl-jingle-slide", "Slide Whistle", "🛝", "#ffb703", "21_Slide-Whistle.mp3", "Slide Whistle")),
            Folder("lupl-fun", "Fun", "😄", "#e9c46a",
                Local("lupl-fun-crickets", "Grillen", "🦗", "#606c38", "01_Awkward-Silence_Crickets.mp3", "Crickets"),
                Local("lupl-fun-badumtss", "Ba-dum-tss", "🥁", "#bc6c25", "02_Schlechter-Witz_Ba-dum-tss.mp3", "Rimshot"),
                Local("lupl-fun-trombone", "Sad Trombone", "🎺", "#6d597a", "09_Skepsis_Sad-Trombone.mp3", "Womp womp"),
                Local("lupl-fun-scratch", "Record Scratch", "💿", "#264653", "14_Record-Scratch.mp3", "Scratch"),
                Local("lupl-fun-womp", "Womp Womp", "😬", "#9d4edd", "08_Peinlicher-Fehler_Womp-Womp.mp3", "Womp"),
                Local("lupl-fun-dundundun", "Dun Dun Dun", "😱", "#e63946", "06_Dramatik_Dun-dun-dun.mp3", "Dramatik")),
        ]),
        new ShowProfile("u21", "U21", "#C8102E",
        [
            Local("u21-goal", "Tor!", "🥅", "#C8102E", "17_Boxing-Gong.mp3", "Boxing Gong", TileSize.Big),
            Local("u21-applause", "Applaus", "👏", "#2a9d8f", "15_Applaus.mp3", "Applaus"),
            Local("u21-buzzer", "Buzzer", "🚨", "#f4a261", "11_Stopp-Fokus_Buzzer.mp3", "Buzzer"),
            Random("u21-random", "Überraschung", "🎲", "#e07a1f", "Zufälliger Fun-Sound", TileSize.Normal, FunPool),
            Folder("u21-einlauf", "Einlauf", "🎵", "#1db954",
                Spot("u21-einlauf-sandstorm", "Sandstorm", "🌪️", "#1db954", "spotify:track:6Sy9BUbgFse0n0LPA5lwy5", 44, 60, "Darude · ab 0:44")),
        ]),
        new ShowProfile("u17", "U17", "#C8102E",
        [
            Local("u17-goal", "Tor!", "🥅", "#C8102E", "17_Boxing-Gong.mp3", "Boxing Gong", TileSize.Big),
            Local("u17-applause", "Applaus", "👏", "#2a9d8f", "15_Applaus.mp3", "Applaus"),
            Random("u17-random", "Überraschung", "🎲", "#e07a1f", "Zufälliger Fun-Sound", TileSize.Normal, FunPool),
            Folder("u17-fun", "Fun", "😄", "#e9c46a",
                Local("u17-fun-coin", "Mario Coin", "🪙", "#e9c46a", "22_Mario-Coin.mp3", "Coin"),
                Local("u17-fun-tada", "Ta-da!", "🎉", "#2a9d8f", "04_Erfolg_Ta-da.mp3", "Ta-da")),
        ]),
    ];
}
