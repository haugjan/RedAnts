namespace TcuConsole;

public class TcuGameState
{
    public string HomeTeam      { get; set; } = "HEIM";
    public string AwayTeam      { get; set; } = "GAST";
    public string HomeTeamShort { get; set; } = "HEI";
    public string AwayTeamShort { get; set; } = "GAS";
    public string Commentator1  { get; set; } = "";
    public string Commentator2  { get; set; } = "";
    public string Referee1      { get; set; } = "";
    public string Referee2      { get; set; } = "";
    public string HomeCoach     { get; set; } = "";
    public string AwayCoach     { get; set; } = "";
    // Rohwert des "00="-Eintrags aus der Spielkonfig. Vermutlich der Topscorer
    // (TCunihockey kennt ein ImgTopscorer in der Titelgrafik) — nicht bestätigt.
    // Bewusst getrennt von den Spielerlisten geführt.
    public string HomeTopScorer { get; set; } = "";
    public string AwayTopScorer { get; set; } = "";
    public List<Player> HomePlayers { get; set; } = [];
    public List<Player> AwayPlayers { get; set; } = [];

    /// <summary>
    /// Nummern der Startaufstellung, in der Reihenfolge, in der TCunihockey sie
    /// einblendet — z.B. "46,6,00,10,12,14". Als Text, nicht als Zahl: aus "00"
    /// würde sonst "0", und der Spieler wäre nicht mehr zuzuordnen.
    ///
    /// Vorher zeigte die Starting-6-Seite schlicht die ersten sechs des Kaders.
    /// Von den sechs stimmten im Beispielspiel zwei.
    /// </summary>
    public List<string> HomeStarting6 { get; set; } = [];
    public List<string> AwayStarting6 { get; set; } = [];

    /// <summary>Die Spieler der Startaufstellung, aufgelöst über die Nummern.
    /// Unbekannte Nummern werden übersprungen.</summary>
    public List<Player> Starting6(string side)
    {
        var nummern = side.Equals("away", StringComparison.OrdinalIgnoreCase) ? AwayStarting6 : HomeStarting6;
        var kader   = side.Equals("away", StringComparison.OrdinalIgnoreCase) ? AwayPlayers   : HomePlayers;

        return nummern
            .Select(nr => kader.FirstOrDefault(
                p => p.Display.Equals(nr, StringComparison.OrdinalIgnoreCase)))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();
    }
    public DateTime? LoadedAt   { get; set; }

    /// <summary>Beschriftung der neun Shortcut-Knöpfe im Bereich ALLGEMEIN,
    /// leer für unbelegte. Wird aus dem Fenster von TCunihockey gelesen, weil
    /// sie aus dessen System-Konfig stammt.</summary>
    public List<string> Shortcuts { get; set; } = [];

    /// <summary>Beschriftung der acht Karten (System-Konfig: ButtonLabel).</summary>
    public List<string> Cards { get; set; } = [];

    /// <summary>Zuschauerzahl. TCunihockey führt keinen Zähler — die Zahl ist
    /// eine frei erfasste Mitteilung im Bereich ALLGEMEIN. Der Zähler lebt
    /// deshalb hier und wird bei jeder Änderung ins Mitteilungsfeld
    /// geschrieben.</summary>
    public int Spectators { get; set; }

    public Player? FindPlayer(string side, int nr)
    {
        var list = side.Equals("away", StringComparison.OrdinalIgnoreCase)
            ? AwayPlayers : HomePlayers;
        return list.FirstOrDefault(p => p.Nr == nr);
    }

    public object ToDto() => new
    {
        homeTeam      = HomeTeam,
        awayTeam      = AwayTeam,
        homeTeamShort = HomeTeamShort,
        awayTeamShort = AwayTeamShort,
        commentator1  = Commentator1,
        commentator2  = Commentator2,
        referee1      = Referee1,
        referee2      = Referee2,
        homeCoach     = HomeCoach,
        awayCoach     = AwayCoach,
        homeTopScorer = HomeTopScorer,
        awayTopScorer = AwayTopScorer,
        homePlayers   = HomePlayers,
        awayPlayers   = AwayPlayers,
        homeStarting6 = HomeStarting6,
        awayStarting6 = AwayStarting6,
        shortcuts     = Shortcuts,
        cards         = Cards,
        spectators    = Spectators,
        loadedAt      = LoadedAt,
    };
}
