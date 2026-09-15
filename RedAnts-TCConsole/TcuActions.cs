namespace TcuConsole;

/// <summary>
/// Übersetzt eine Aktion in die Befehle, die TCunihockey versteht.
///
/// Zwei Sorten Befehl, unterschieden am Präfix:
///   "TcuController=" geht per UDP direkt an TCunihockey (Port 7001).
///   "TcuUi="         geht an TcuUi und wird als Klick im Fenster ausgeführt.
///
/// Die zweite Sorte gibt es nur, weil TCunihockey 6.2.10 für einen Teil seiner
/// Funktionen schlicht keinen UDP-Befehl kennt — Strafen, Karten, Shortcuts,
/// die freie Mitteilung und "Highlight Resultat". Der UDP-Befehlssatz ist
/// abschliessend bekannt (51 TcuController= + 11 TcuExternal= im Binary);
/// alles darin wird hier auch über UDP gefahren, nichts über die Oberfläche.
/// </summary>
public static class TcuActions
{
    const string P = "TcuController=";
    const string U = "TcuUi=";

    // Jede Einblendung beginnt mit Ausblenden: lowerthird_show ersetzt eine
    // laufende Einblendung nicht, und hide ist idempotent. Dieselbe Regel wie
    // in CompanionConfig.
    const string Hide = $"{P}lowerthird_hide";
    const string Show = $"{P}lowerthird_show";

    static string[] Seq(params string[] cmds)     => [Hide, .. cmds];
    static string[] SeqShow(params string[] cmds) => [Hide, .. cmds, Show];

    public static string[] Resolve(ActionRequest req, TcuGameState state)
    {
        var side = req.Side?.Equals("away", StringComparison.OrdinalIgnoreCase) == true
            ? "away" : "home";
        var nr = req.Nr;

        return req.Type?.ToLower() switch
        {
            // ── Lower third: player events (brauchen Spielernummer) ──────────────
            "tor" when nr.HasValue => SeqShow(
                $"{P}lowerthird_{side}_goal+1",
                $"{P}lowerthird_{side}_player|{nr}"),
            "name" when nr.HasValue => SeqShow(
                $"{P}lowerthird_{side}_name",
                $"{P}lowerthird_{side}_player|{nr}"),
            "bestplayer" when nr.HasValue => SeqShow(
                $"{P}lowerthird_{side}_bestplayer",
                $"{P}lowerthird_{side}_player|{nr}"),

            // ── Lower third: Ereignisse ohne Spielernummer ───────────────────────
            "coach"     => SeqShow($"{P}lowerthird_{side}_coach"),
            "timeout"   => SeqShow($"{P}lowerthird_{side}_timeout"),
            "lineup"    => SeqShow($"{P}lowerthird_{side}_lineup"),
            "starting6" => SeqShow($"{P}lowerthird_{side}_starting6"),

            // ── Lower third: fixes ───────────────────────────────────────────────
            "commentary" => SeqShow($"{P}lowerthird_commentary12"),
            "referee"    => SeqShow($"{P}lowerthird_referee12"),
            "opener"     => SeqShow($"{P}lowerthird_opener"),
            "result"     => SeqShow($"{P}lowerthird_result"),
            "hide"       => [Hide],
            "next"       => [$"{P}lowerthird_next"],

            // ── Scoreboard ───────────────────────────────────────────────────────
            "scoreboard_toggle"     => [$"{P}scoreboard_togglestartstop"],
            "scoreboard_start"      => [$"{P}scoreboard_start"],
            "scoreboard_stop"       => [$"{P}scoreboard_stop"],
            "scoreboard_show"       => [$"{P}scoreboard_show"],
            "scoreboard_hide"       => [$"{P}scoreboard_hide"],
            "scoreboard_toggleshow" => [$"{P}scoreboard_toggleshowhide"],
            "scoreboard_secplus"    => [$"{P}scoreboard_secplus"],
            "scoreboard_secmin"     => [$"{P}scoreboard_secmin"],
            "scoreboard_psreset"    => [$"{P}scoreboard_penaltyshots_reset"],
            "period_1"              => [$"{P}scoreboard_period|1"],
            "period_2"              => [$"{P}scoreboard_period|2"],
            "period_3"              => [$"{P}scoreboard_period|3"],
            "period_ot"             => [$"{P}scoreboard_period|O"],
            "period_ps"             => [$"{P}scoreboard_period|P"],

            // ── Sponsor ──────────────────────────────────────────────────────────
            "sponsor_toggle" => [$"{P}sponsor_toggleshowhide"],
            "sponsor_show"   => [$"{P}sponsor_show"],
            "sponsor_hide"   => [$"{P}sponsor_hide"],
            "sponsor_auto"   => [$"{P}sponsor_auto_toggle"],

            // ── Ohne UDP-Befehl: Klick im Fenster ────────────────────────────────
            //
            // Strafe setzt nur den Modus (Seite + Dauer). Die Spielerwahl läuft
            // danach wieder über UDP — lowerthird_{side}_player|{nr} wählt den
            // Spieler für den eingestellten Modus, egal wie der gesetzt wurde.
            // Deshalb hier bewusst kein lowerthird_show: das kommt erst mit dem
            // Spieler.
            // Kein Show in diesen Ketten: der Klick geht über TcuUi und braucht
            // gemessen 265–450 ms. Ein hier eingeplantes lowerthird_show träfe
            // TCunihockey unter Umständen vor der Wirkung des Klicks — dann wird
            // der alte Inhalt live geschaltet und der Klick nimmt ihn wieder von
            // der Anzeige. Das Einblenden übernimmt der Aufrufer, sobald der
            // Klick zurückgemeldet hat (siehe TcuUi.NeedsShow).
            "strafe" when req.Penalty is not null =>
                Seq($"{U}penalty|{side}|{req.Penalty}"),

            "card"      when nr.HasValue => Seq($"{U}card|{nr}"),
            "shortcut"  when nr.HasValue => Seq($"{U}shortcut|{nr}"),
            "highlight"                  => Seq($"{U}highlight_result"),

            "message" when !string.IsNullOrWhiteSpace(req.Text) =>
                Seq($"{U}message|{req.Text}"),

            // Zuschauerzahl: nur zählen und ins Mitteilungsfeld schreiben,
            // nicht live schalten — sonst springt die Einblendung bei jedem
            // Tastendruck auf Sendung.
            "spectators" when nr.HasValue =>
                [$"{U}spectators|{nr}"],

            "spectators_show" => Seq($"{U}spectators|show"),

            _ => []
        };
    }
}
