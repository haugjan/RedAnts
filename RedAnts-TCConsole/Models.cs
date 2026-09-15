namespace TcuConsole;

/// <summary>
/// Eine Aktion aus Companion. <paramref name="Nr"/> ist je nach Typ die
/// Spielernummer, der Index einer Karte/eines Shortcuts oder die Änderung der
/// Zuschauerzahl (darf negativ sein).
/// </summary>
public record ActionRequest(
    string Type, string? Side = null, int? Nr = null,
    string? Penalty = null, string? Text = null);
public record RawRequest(string Command);
public record LoadFileRequest(string Path);

public class Player
{
    /// <summary>Nummer als Zahl — nur zum Sortieren. "00" wird hier zu 0.</summary>
    public int Nr { get; set; }

    /// <summary>Nummer wie TCunihockey sie führt, inklusive führender Nullen.
    /// Muss für Befehle und Beschriftungen verwendet werden, sonst wird aus
    /// "00" ein "0" und der Spieler ist nicht ansprechbar.</summary>
    public string NrText { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Anzuzeigende Nummer; fällt auf Nr zurück, falls NrText leer ist.</summary>
    public string Display => NrText.Length > 0 ? NrText : Nr.ToString();

    public override string ToString() => $"{Display} {Name}";
}
