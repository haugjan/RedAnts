namespace RedAnts.Game.Domain;

public static class PointsOrigin
{
    private static readonly (string Key, string Label)[] Known =
    [
        ("einsatz", "Einsatz"),
        ("tore", "Tore"),
        ("assists", "Assists"),
        ("resultat", "Resultat"),
        ("gegentore", "Gegentore des Teams"),
        ("bestplayer", "Bestplayer"),
        ("strafen", "Strafen"),
    ];

    public static string Label(string key)
    {
        foreach (var (known, label) in Known)
            if (string.Equals(known, key, StringComparison.OrdinalIgnoreCase)) return label;
        return key;
    }

    public static int Order(string key)
    {
        for (var i = 0; i < Known.Length; i++)
            if (string.Equals(Known[i].Key, key, StringComparison.OrdinalIgnoreCase)) return i;
        return Known.Length;
    }
}
