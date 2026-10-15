namespace RedAnts.Show.Domain;

public static class DeckIcons
{
    public const string Folder = "/_content/RedAnts.Show/show/icons/";

    public static IReadOnlyList<string> All { get; } =
    [
        "aufstellung",
        "ball",
        "bestplayer",
        "coach",
        "drittel",
        "einblender-aus",
        "einblender-ein",
        "highlights",
        "kommentar",
        "kommentar-b",
        "meldungen",
        "opener",
        "resultat",
        "schiedsrichter",
        "schiedsrichter-b",
        "spieler",
        "starting6",
        "strafe",
        "timeout",
        "werbung",
    ];

    public static string FileOf(string icon) => Folder + icon + ".png";
}
